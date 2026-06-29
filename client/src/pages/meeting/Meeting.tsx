import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Loader2, Mic, MicOff, PhoneOff, Video, VideoOff } from "lucide-react";
import {
  CallClient,
  LocalVideoStream,
  VideoStreamRenderer,
  type Call,
  type CallAgent,
  type RemoteParticipant,
  type RemoteVideoStream,
  type VideoStreamRendererView,
} from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";
import { meetingsApi } from "@/services/api/meetings";
import { ApiError, apiErrorMessage } from "@/services/api/client";
import { useAuthStore } from "@/stores/authStore";

type Phase = "joining" | "connected" | "ended" | "error";

/**
 * In-app video session for a consultant booking (PB-006). Joins the booking's
 * Azure Communication Services group call. Opening this page also records the
 * participant's attendance, which the no-show sweep (FR-217) reads.
 */
export function Meeting() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation(["bookings", "common"]);
  const userFullName = useAuthStore((s) => s.user?.fullName);
  const displayName = userFullName?.trim() || t("bookings:meeting.you");

  const [phase, setPhase] = useState<Phase>("joining");
  const [errorMsg, setErrorMsg] = useState("");
  const [micOn, setMicOn] = useState(true);
  const [camOn, setCamOn] = useState(true);
  const [remoteJoined, setRemoteJoined] = useState(false);
  const [hasCamera, setHasCamera] = useState(false);
  const [recording, setRecording] = useState(false);

  const localVideoRef = useRef<HTMLDivElement>(null);
  const remoteVideoRef = useRef<HTMLDivElement>(null);

  // SDK handles kept in refs so the controls and cleanup can reach them.
  const callRef = useRef<Call | null>(null);
  const agentRef = useRef<CallAgent | null>(null);
  const localStreamRef = useRef<LocalVideoStream | null>(null);
  const viewsRef = useRef<VideoStreamRendererView[]>([]);

  useEffect(() => {
    if (!bookingId) return;
    let disposed = false;
    const views = viewsRef.current;

    async function renderRemote(stream: RemoteVideoStream) {
      try {
        if (disposed || !stream.isAvailable) return;
        const view = await new VideoStreamRenderer(stream).createView();
        if (disposed) {
          view.dispose();
          return;
        }
        views.push(view);
        if (remoteVideoRef.current) {
          remoteVideoRef.current.replaceChildren(view.target);
        }
        setRemoteJoined(true);
      } catch {
        /* stream not renderable yet */
      }
    }

    function watchStream(stream: RemoteVideoStream) {
      stream.on("isAvailableChanged", () => {
        if (stream.isAvailable) void renderRemote(stream);
      });
      if (stream.isAvailable) void renderRemote(stream);
    }

    function watchParticipant(participant: RemoteParticipant) {
      participant.videoStreams.forEach(watchStream);
      participant.on("videoStreamsUpdated", (e) => e.added.forEach(watchStream));
    }

    async function start() {
      try {
        const join = await meetingsApi.join(bookingId!);
        if (disposed) return;

        // When ACS isn't configured the server issues a stub token. Feeding it to
        // the ACS SDK throws an opaque credential error, so surface an honest
        // "video not configured" message instead of a broken-looking call.
        if (join.provider === "Stub") {
          throw new Error(t("bookings:meeting.errorNotConfigured"));
        }

        const callClient = new CallClient();
        const credential = new AzureCommunicationTokenCredential(join.accessToken);
        const agent = await callClient.createCallAgent(credential, { displayName });
        if (disposed) {
          await agent.dispose().catch(() => undefined);
          return;
        }
        agentRef.current = agent;

        const deviceManager = await callClient.getDeviceManager();
        try {
          await deviceManager.askDevicePermission({ video: true, audio: true });
        } catch (permErr) {
          // User denied camera/mic — surface a specific message rather than the
          // generic error. The call cannot proceed without at least audio.
          if (
            permErr instanceof Error
            && (permErr.name === "NotAllowedError" || permErr.name === "PermissionDeniedError")
          ) {
            throw new Error(t("bookings:meeting.errorPermissionDenied"));
          }
          throw permErr;
        }
        const cameras = await deviceManager.getCameras();

        let localStream: LocalVideoStream | null = null;
        const camera = cameras.at(0);
        if (camera) {
          localStream = new LocalVideoStream(camera);
          localStreamRef.current = localStream;
          setHasCamera(true);
        } else {
          setCamOn(false);
        }

        const call = agent.join(
          { groupId: join.roomId },
          {
            videoOptions: { localVideoStreams: localStream ? [localStream] : [] },
            audioOptions: { muted: false },
          },
        );
        if (disposed) {
          await call.hangUp().catch(() => undefined);
          return;
        }
        callRef.current = call;

        if (localStream && localVideoRef.current) {
          const view = await new VideoStreamRenderer(localStream).createView();
          if (disposed) {
            view.dispose();
            return;
          }
          views.push(view);
          localVideoRef.current.replaceChildren(view.target);
        }


        call.remoteParticipants.forEach(watchParticipant);
        call.on("remoteParticipantsUpdated", (e) => {
          e.added.forEach(watchParticipant);
          if (call.remoteParticipants.length === 0) setRemoteJoined(false);
        });
        // Start the session recording once the call is connected (PB-006).
        // Idempotent server-side, so it is safe if both participants fire it.
        let recordingStarted = false;
        async function ensureRecording() {
          if (recordingStarted || disposed) return;
          recordingStarted = true;
          try {
            const serverCallId = await call.info.getServerCallId();
            await meetingsApi.startRecording(bookingId!, serverCallId);
            if (!disposed) setRecording(true);
          } catch {
            recordingStarted = false; // allow a retry on the next state change
          }
        }

        call.on("stateChanged", () => {
          if (disposed) return;
          if (call.state === "Connected") void ensureRecording();
          if (call.state === "Disconnected") {
            setPhase("ended");
            // The call ended while the user is still on the page — release the
            // agent immediately so the SDK does not hold the mic/camera while
            // the user reads the "session ended" screen. Cleanup will still run
            // on navigation; nulling refs here prevents a double-dispose throw.
            const a = agentRef.current;
            agentRef.current = null;
            callRef.current = null;
            void a?.dispose().catch(() => undefined);
          }
        });
        if (call.state === "Connected") void ensureRecording();

        if (!disposed) setPhase("connected");
      } catch (err) {
        if (!disposed) {
          setErrorMsg(resolveErrorMessage(err));
          setPhase("error");
        }
      }
    }

    function resolveErrorMessage(err: unknown): string {
      // The browser permission flow and stub-provider path both throw a localised
      // Error directly — keep the message rather than re-translating.
      if (err instanceof Error) {
        if (
          err.message === t("bookings:meeting.errorPermissionDenied") ||
          err.message === t("bookings:meeting.errorNotConfigured")
        ) {
          return err.message;
        }
      }
      if (err instanceof ApiError) {
        // 403 — the authenticated user is not a participant of this booking.
        if (err.status === 403) {
          return t("bookings:meeting.errorNotParticipant");
        }
        const detail = err.payload.detail ?? "";
        const lower = detail.toLowerCase();
        if (lower.includes("not open yet")) {
          return t("bookings:meeting.errorNotOpenYet");
        }
        if (lower.includes("has closed")) {
          return t("bookings:meeting.errorRoomClosed");
        }
        if (lower.includes("confirmed booking")) {
          return t("bookings:meeting.errorNotConfirmed");
        }
        if (detail) return detail;
      }
      return apiErrorMessage(err, "") || t("bookings:meeting.errorGeneric");
    }

    void start();

    return () => {
      disposed = true;
      views.forEach((v) => {
        try {
          v.dispose();
        } catch {
          /* already disposed */
        }
      });
      viewsRef.current = [];

      // Snapshot then null the refs before awaiting disposal — a fast remount
      // (React strict-mode dev double-mount, rapid route change) must not
      // re-enter dispose() on an already-disposed handle.
      const call = callRef.current;
      const agent = agentRef.current;
      callRef.current = null;
      agentRef.current = null;
      localStreamRef.current = null;
      void call?.hangUp().catch(() => undefined);
      void agent?.dispose().catch(() => undefined);
    };
  // `t` from useTranslation is referentially stable (never changes across
  // renders), so adding it to deps does not cause extra effect runs.
  }, [bookingId, displayName, t]);

  const toggleMic = useCallback(async () => {
    const call = callRef.current;
    if (!call) return;
    try {
      await (micOn ? call.mute() : call.unmute());
      setMicOn((v) => !v);
    } catch {
      /* transient SDK state */
    }
  }, [micOn]);

  const toggleCam = useCallback(async () => {
    const call = callRef.current;
    const stream = localStreamRef.current;
    if (!call || !stream) return;
    try {
      await (camOn ? call.stopVideo(stream) : call.startVideo(stream));
      setCamOn((v) => !v);
    } catch {
      /* transient SDK state */
    }
  }, [camOn]);

  const leave = useCallback(() => {
    // Hang up here too (in addition to effect cleanup) so the SDK starts
    // tearing the call down before the route transition rather than after —
    // the user sees the mic light go off the moment they click Leave.
    const call = callRef.current;
    callRef.current = null;
    void call?.hangUp().catch(() => undefined);
    navigate(-1);
  }, [navigate]);

  return (
    <main className="flex min-h-screen flex-col bg-neutral-950 text-white">
      <header className="flex items-center justify-between px-5 py-3">
        <h1 className="text-sm font-medium">{t("bookings:meeting.title")}</h1>
        <div className="flex items-center gap-3 text-xs">
          {recording && (
            <span className="flex items-center gap-1.5 text-danger-400">
              <span aria-hidden className="size-2 animate-pulse rounded-full bg-danger-500" />
              {t("bookings:meeting.recording")}
            </span>
          )}
          <span className="text-neutral-400">
            {phase === "connected"
              ? remoteJoined
                ? t("bookings:meeting.connected")
                : t("bookings:meeting.waiting")
              : null}
          </span>
        </div>
      </header>
      {phase === "connected" && (
        <p className="px-5 pb-1 text-[11px] text-neutral-500">
          {t("bookings:meeting.recordingNotice")}
        </p>
      )}

      <div className="relative flex flex-1 items-center justify-center overflow-hidden px-4 pb-4">
        {phase === "joining" && (
          <div className="flex flex-col items-center gap-3 text-neutral-300">
            <Loader2 aria-hidden className="size-8 animate-spin" />
            <p className="text-sm">{t("bookings:meeting.joining")}</p>
          </div>
        )}

        {phase === "error" && (
          <div className="max-w-md text-center">
            <p className="text-sm text-neutral-200">
              {errorMsg || t("bookings:meeting.errorGeneric")}
            </p>
            <button
              type="button"
              onClick={() => navigate(-1)}
              className="mt-5 rounded-lg bg-white/10 px-5 py-2 text-sm font-medium hover:bg-white/20"
            >
              {t("bookings:meeting.back")}
            </button>
          </div>
        )}

        {phase === "ended" && (
          <div className="max-w-md text-center">
            <p className="text-sm text-neutral-200">{t("bookings:meeting.ended")}</p>
            <button
              type="button"
              onClick={() => navigate(-1)}
              className="mt-5 rounded-lg bg-white/10 px-5 py-2 text-sm font-medium hover:bg-white/20"
            >
              {t("bookings:meeting.back")}
            </button>
          </div>
        )}

        {/* The video surfaces stay mounted while connected so the SDK can
            attach its renderers to the ref containers. */}
        <div className={phase === "connected" ? "h-full w-full" : "hidden"}>
          <div
            ref={remoteVideoRef}
            className="flex h-full w-full items-center justify-center rounded-xl bg-neutral-900"
          >
            {!remoteJoined && (
              <p className="text-sm text-neutral-500">{t("bookings:meeting.waiting")}</p>
            )}
          </div>
          <div
            ref={localVideoRef}
            aria-label={t("bookings:meeting.you")}
            className="absolute bottom-24 end-8 h-36 w-56 overflow-hidden rounded-lg border border-white/15 bg-neutral-800"
          />
        </div>
      </div>

      {phase === "connected" && (
        <div className="flex items-center justify-center gap-3 pb-6">
          <button
            type="button"
            onClick={() => void toggleMic()}
            aria-label={micOn ? t("bookings:meeting.micOff") : t("bookings:meeting.micOn")}
            className="flex size-12 items-center justify-center rounded-full bg-white/10 hover:bg-white/20"
          >
            {micOn ? <Mic aria-hidden className="size-5" /> : <MicOff aria-hidden className="size-5" />}
          </button>
          <button
            type="button"
            onClick={() => void toggleCam()}
            aria-label={camOn ? t("bookings:meeting.camOff") : t("bookings:meeting.camOn")}
            className="flex size-12 items-center justify-center rounded-full bg-white/10 hover:bg-white/20 disabled:opacity-40"
            disabled={!hasCamera}
          >
            {camOn ? <Video aria-hidden className="size-5" /> : <VideoOff aria-hidden className="size-5" />}
          </button>
          <button
            type="button"
            onClick={leave}
            aria-label={t("bookings:meeting.leave")}
            className="flex size-12 items-center justify-center rounded-full bg-danger-500 hover:bg-danger-600"
          >
            <PhoneOff aria-hidden className="size-5" />
          </button>
        </div>
      )}
    </main>
  );
}
