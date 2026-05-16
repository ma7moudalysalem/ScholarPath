import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import {
  applicationsApi,
  type ApplicationListItem,
  type ApplicationDetail,
  type StartApplicationRequest,
  type WithdrawApplicationRequest,
  type UpdateExternalStatusRequest,
} from "@/services/api/applications";

// ── Queries ───────────────────────────────────────────────────────────────────

export function useMyApplicationsQuery() {
  return useQuery<ApplicationListItem[]>({
    queryKey: queryKeys.applications.mine,
    queryFn:  () => applicationsApi.getMyApplications(),
    staleTime: 30_000,
  });
}

export function useApplicationDetailQuery(id: string | undefined) {
  return useQuery<ApplicationDetail>({
    queryKey: queryKeys.applications.detail(id ?? ""),
    queryFn:  () => applicationsApi.getById(id ?? ""),
    enabled:  !!id,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useStartApplicationMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: StartApplicationRequest) =>
      applicationsApi.start(req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.applications.all });
    },
  });
}

export function useSubmitApplicationMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => applicationsApi.submit(id),
    onSuccess: (_data, id) => {
      void qc.invalidateQueries({ queryKey: queryKeys.applications.detail(id) });
      void qc.invalidateQueries({ queryKey: queryKeys.applications.mine });
    },
  });
}

export function useWithdrawApplicationMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: WithdrawApplicationRequest }) =>
      applicationsApi.withdraw(id, req),
    onSuccess: (_data, { id }) => {
      void qc.invalidateQueries({ queryKey: queryKeys.applications.detail(id) });
      void qc.invalidateQueries({ queryKey: queryKeys.applications.mine });
    },
  });
}

export function useUpdateExternalStatusMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      req,
    }: {
      id: string;
      req: UpdateExternalStatusRequest;
    }) => applicationsApi.updateExternalStatus(id, req),
    onSuccess: (_data, { id }) => {
      void qc.invalidateQueries({ queryKey: queryKeys.applications.detail(id) });
      void qc.invalidateQueries({ queryKey: queryKeys.applications.mine });
    },
  });
}
