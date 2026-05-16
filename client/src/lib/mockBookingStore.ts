export type BookingWorkflowStatus =
  | "pending"
  | "confirmed"
  | "completed"
  | "rejected"
  | "cancelled";

export type MockBookingRecord = {
  id: string;
  reference: string;
  consultantId: string;
  consultantName: string;
  studentName: string;
  studentEmail: string;
  topic: string;
  studentStage: string;
  sessionType: string;
  date: string;
  time: string;
  duration: string;
  fee: string;
  status: BookingWorkflowStatus;
};

type CreateMockBookingInput = {
  reference: string;
  consultantId: string;
  consultantName: string;
  studentName: string;
  studentEmail: string;
  topic: string;
  studentStage: string;
  sessionType: string;
  date: string;
  time: string;
  duration: string;
  fee: string;
};

type Subscriber = () => void;

const initialBookings: MockBookingRecord[] = [
  {
    id: "1",
    reference: "BK-1-SLOT-1",
    consultantId: "1",
    consultantName: "Dr. Sarah Adel",
    studentName: "Tasneem Shaban",
    studentEmail: "tasneem@example.com",
    topic: "Scholarship strategy and essay review",
    studentStage: "Preparing shortlist and refining personal statement",
    sessionType: "1:1 online consultation",
    date: "25 Apr 2026",
    time: "6:30 PM",
    duration: "45 min",
    fee: "$35.00",
    status: "pending",
  },
  {
    id: "2",
    reference: "BK-2-SLOT-3",
    consultantId: "2",
    consultantName: "Ahmed Mostafa",
    studentName: "Mariam Adel",
    studentEmail: "mariam@example.com",
    topic: "Visa guidance and university shortlist",
    studentStage: "Preparing visa documents and narrowing target universities",
    sessionType: "1:1 online consultation",
    date: "27 Apr 2026",
    time: "12:30 PM",
    duration: "30 min",
    fee: "$25.00",
    status: "confirmed",
  },
  {
    id: "3",
    reference: "BK-3-SLOT-2",
    consultantId: "3",
    consultantName: "Nour Elhassan",
    studentName: "Nourhan Ali",
    studentEmail: "nourhan@example.com",
    topic: "Full scholarship planning review",
    studentStage: "Finalizing funding strategy and deadline planning",
    sessionType: "1:1 online consultation",
    date: "28 Apr 2026",
    time: "2:00 PM",
    duration: "60 min",
    fee: "$40.00",
    status: "completed",
  },
  {
    id: "4",
    reference: "BK-1-SLOT-5",
    consultantId: "1",
    consultantName: "Dr. Sarah Adel",
    studentName: "Youssef Hany",
    studentEmail: "youssef@example.com",
    topic: "Interview preparation session",
    studentStage: "Preparing for scholarship interview round",
    sessionType: "1:1 online consultation",
    date: "29 Apr 2026",
    time: "7:00 PM",
    duration: "45 min",
    fee: "$35.00",
    status: "rejected",
  },
  {
    id: "5",
    reference: "BK-2-SLOT-4",
    consultantId: "2",
    consultantName: "Ahmed Mostafa",
    studentName: "Salma Tarek",
    studentEmail: "salma@example.com",
    topic: "Funding plan follow-up",
    studentStage: "Comparing funding options and timeline readiness",
    sessionType: "1:1 online consultation",
    date: "29 Apr 2026",
    time: "6:00 PM",
    duration: "30 min",
    fee: "$25.00",
    status: "cancelled",
  },
];

let bookings: MockBookingRecord[] = [...initialBookings];
const subscribers = new Set<Subscriber>();

function notifySubscribers() {
  subscribers.forEach((subscriber) => subscriber());
}

export function getMockBookings() {
  return [...bookings];
}

export function getMockBookingById(id: string) {
  return bookings.find((item) => item.id === id) ?? bookings[0];
}

export function subscribeMockBookings(subscriber: Subscriber) {
  subscribers.add(subscriber);

  return () => {
    subscribers.delete(subscriber);
  };
}

export function setMockBookingStatus(id: string, status: BookingWorkflowStatus) {
  bookings = bookings.map((item) =>
    item.id === id
      ? {
          ...item,
          status,
        }
      : item,
  );

  notifySubscribers();

  return getMockBookingById(id);
}

export function createMockBookingRequest(input: CreateMockBookingInput) {
  const nextId = String(bookings.reduce((max, item) => Math.max(max, Number(item.id)), 0) + 1);

  const created: MockBookingRecord = {
    id: nextId,
    reference: input.reference,
    consultantId: input.consultantId,
    consultantName: input.consultantName,
    studentName: input.studentName,
    studentEmail: input.studentEmail,
    topic: input.topic,
    studentStage: input.studentStage,
    sessionType: input.sessionType,
    date: input.date,
    time: input.time,
    duration: input.duration,
    fee: input.fee,
    status: "pending",
  };

  bookings = [created, ...bookings];
  notifySubscribers();

  return created;
}

export function resetMockBookings() {
  bookings = [...initialBookings];
  notifySubscribers();
  return [...bookings];
}
