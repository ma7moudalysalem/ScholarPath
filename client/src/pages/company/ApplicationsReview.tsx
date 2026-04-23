import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle, XCircle, Eye, Clock, Search, Filter } from 'lucide-react';
import { applicationsApi } from '@/services/api/applications';
import { queryKeys } from '@/lib/queryClient';

export function ApplicationsReview() {
  const { t } = useTranslation('applications');
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');

  const { data: applications = [], isLoading } = useQuery({
    queryKey: ['company', 'applications'],
    queryFn: () => applicationsApi.getCompanyApplications(),
  });

  const reviewMutation = useMutation({
    mutationFn: ({ id, status, reason }: { id: string; status: any; reason?: string }) =>
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
    if (reason === null) return; // User cancelled prompt
    reviewMutation.mutate({ id, status, reason });
  };

  const filteredApps = applications.filter((app: any) => 
    app.studentName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    app.scholarshipTitle?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="mx-auto max-w-7xl p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
          Review Applications
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Manage and review incoming scholarship applications.
        </p>
      </div>

      <div className="mb-6 flex flex-col space-y-4 md:flex-row md:items-center md:justify-between md:space-y-0">
        <div className="relative w-full max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
          <input
            type="text"
            placeholder="Search students or scholarships..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-10 pr-4 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-800 dark:bg-slate-900 dark:text-white"
          />
        </div>
        
        <button className="flex items-center space-x-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300">
          <Filter size={18} />
          <span>Filters</span>
        </button>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs font-semibold uppercase text-slate-500 dark:bg-slate-800/50 dark:text-slate-400">
              <tr>
                <th className="px-6 py-4">Student</th>
                <th className="px-6 py-4">Scholarship</th>
                <th className="px-6 py-4">Status</th>
                <th className="px-6 py-4">Submitted</th>
                <th className="px-6 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center">
                    <div className="flex justify-center">
                      <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary-500 border-t-transparent" />
                    </div>
                  </td>
                </tr>
              ) : filteredApps.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-slate-500">
                    No applications found.
                  </td>
                </tr>
              ) : (
                filteredApps.map((app: any) => (
                  <tr key={app.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-medium text-slate-900 dark:text-white">{app.studentName}</div>
                      <div className="text-xs text-slate-500">{app.studentEmail}</div>
                    </td>
                    <td className="px-6 py-4 text-slate-600 dark:text-slate-300">
                      {app.scholarshipTitle}
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        app.status === 'Accepted' ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400' :
                        app.status === 'Rejected' ? 'bg-rose-100 text-rose-800 dark:bg-rose-900/30 dark:text-rose-400' :
                        'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400'
                      }`}>
                        {app.status === 'Pending' ? <Clock size={12} className="mr-1" /> : null}
                        {app.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-slate-500">
                      {new Date(app.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex justify-end space-x-2">
                        <button className="p-1.5 text-slate-400 hover:text-primary-600 transition-colors" title="View details">
                          <Eye size={18} />
                        </button>
                        {app.status === 'Pending' && (
                          <>
                            <button 
                              onClick={() => handleDecision(app.id, 'Accepted')}
                              className="p-1.5 text-slate-400 hover:text-emerald-600 transition-colors" 
                              title="Accept"
                            >
                              <CheckCircle size={18} />
                            </button>
                            <button 
                              onClick={() => handleDecision(app.id, 'Rejected')}
                              className="p-1.5 text-slate-400 hover:text-rose-600 transition-colors" 
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
