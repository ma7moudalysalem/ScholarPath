import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle, XCircle, Eye, Clock, Search, Filter } from 'lucide-react';
import { applicationsApi, type CompanyApplicationRow, type ApplicationStatus } from '@/services/api/applications';

export function ApplicationsReview() {
  useTranslation('applications');
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['company', 'applications'],
    queryFn: () => applicationsApi.getCompanyApplications(),
  });
  const applications = data?.items ?? [];

  const reviewMutation = useMutation({
    mutationFn: ({ id, status, reason }: { id: string; status: ApplicationStatus; reason?: string }) =>
      applicationsApi.reviewApplication(id, status, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['company', 'applications'] });
      toast.success('Decision recorded successfully');
    },
    onError: () => {
      toast.error('Failed to record decision');
    }
  });

  const handleDecision = (id: string, status: 'Accepted' | 'Rejected') => {
    const reason = window.prompt(status === 'Rejected' ? 'Reason for rejection (optional):' : 'Notes for acceptance (optional):');
    if (reason === null) return;
    reviewMutation.mutate({ id, status, reason });
  };

  const filteredApps = applications.filter((app: CompanyApplicationRow) =>
    app.studentName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    app.studentEmail.toLowerCase().includes(searchTerm.toLowerCase()) ||
    app.scholarshipTitle.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="mx-auto max-w-7xl p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight text-text-primary">
          Review Applications
        </h1>
        <p className="text-sm text-text-secondary">
          Manage and review incoming scholarship applications.
        </p>
      </div>

      <div className="mb-6 flex flex-col space-y-4 md:flex-row md:items-center md:justify-between md:space-y-0">
        <div className="relative w-full max-w-sm">
          <Search className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary" size={18} />
          <input
            type="text"
            placeholder="Search students or scholarships..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full rounded-lg border border-border-subtle bg-bg-elevated py-2 ps-10 pe-4 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-100"
          />
        </div>

        <button className="flex items-center space-x-2 rounded-lg border border-border-subtle bg-bg-elevated px-4 py-2 text-sm font-medium text-text-secondary hover:bg-bg-subtle transition-colors">
          <Filter size={18} />
          <span>Filters</span>
        </button>
      </div>

      <div className="overflow-hidden rounded-xl border border-border-subtle bg-bg-elevated shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-start text-sm">
            <thead className="bg-bg-muted text-xs font-semibold uppercase text-text-tertiary">
              <tr>
                <th className="px-6 py-4">Student</th>
                <th className="px-6 py-4">Scholarship</th>
                <th className="px-6 py-4">Status</th>
                <th className="px-6 py-4">Submitted</th>
                <th className="px-6 py-4 text-end">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center">
                    <div className="flex justify-center">
                      <div className="h-6 w-6 animate-spin rounded-full border-2 border-brand-500 border-t-transparent" />
                    </div>
                  </td>
                </tr>
              ) : filteredApps.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-text-secondary">
                    No applications found.
                  </td>
                </tr>
              ) : (
                filteredApps.map((app: CompanyApplicationRow) => (
                  <tr key={app.id} className="hover:bg-bg-muted/50 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-medium text-text-primary">{app.studentName}</div>
                      <div className="text-xs text-text-tertiary">{app.studentEmail}</div>
                    </td>
                    <td className="px-6 py-4 text-text-secondary">
                      {app.scholarshipTitle}
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        app.status === 'Accepted' ? 'bg-success-50 text-success-700' :
                        app.status === 'Rejected' ? 'bg-danger-50 text-danger-500' :
                        'bg-warning-50 text-warning-600'
                      }`}>
                        {app.status === 'Pending' ? <Clock size={12} className="me-1" /> : null}
                        {app.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-text-tertiary">
                      {new Date(app.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-end">
                      <div className="flex justify-end space-x-2">
                        <button type="button" className="p-1.5 text-text-tertiary hover:text-brand-600 transition-colors" aria-label="View details">
                          <Eye size={18} />
                        </button>
                        {app.status === 'Pending' && (
                          <>
                            <button
                              onClick={() => handleDecision(app.id, 'Accepted')}
                              className="p-1.5 text-text-tertiary hover:text-success-600 transition-colors"
                              title="Accept"
                            >
                              <CheckCircle size={18} />
                            </button>
                            <button
                              onClick={() => handleDecision(app.id, 'Rejected')}
                              className="p-1.5 text-text-tertiary hover:text-danger-500 transition-colors"
                              title="Reject"
                            >
                              <XCircle size={18} />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
