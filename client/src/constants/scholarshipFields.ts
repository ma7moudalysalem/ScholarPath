/**
 * Canonical list of academic fields of study used in scholarship
 * listings and discovery filters. Shared between ScholarshipForm
 * (company create) and ScholarshipsPage (student filter).
 */
export const SCHOLARSHIP_FIELDS_OF_STUDY = [
  "Computer Science",
  "Engineering",
  "Medicine & Health",
  "Business & Management",
  "Economics & Finance",
  "Law",
  "Arts & Humanities",
  "Social Sciences",
  "Natural Sciences",
  "Mathematics & Statistics",
  "Education",
  "Architecture & Design",
  "Agriculture & Environment",
  "Media & Communications",
  "Other",
] as const;

export type ScholarshipFieldOfStudy = (typeof SCHOLARSHIP_FIELDS_OF_STUDY)[number];
