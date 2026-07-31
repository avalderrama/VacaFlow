import { RequestFormHeader } from '@/components/requests/RequestFormHeader';
import { RequestForm } from '@/components/requests/RequestForm';

export default function NewRequestPage() {
  return (
    <div>
      <RequestFormHeader title="New request" />
      <RequestForm mode="create" />
    </div>
  );
}
