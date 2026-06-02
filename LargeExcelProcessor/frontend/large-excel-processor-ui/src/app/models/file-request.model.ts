export interface FileRequest {
  id: string;
  requestType: string;
  status: string;
  fileName: string;
  fileSize: number;
  blobUri?: string;
  resultBlobUri?: string;
  totalRows?: number;
  importedRows?: number;
  errorMessage?: string;
  createdAt: string;
  completedAt?: string;
}
