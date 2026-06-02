export interface InvoiceRecord {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
  vendorName: string;
  vendorTaxId?: string;
  customerName: string;
  customerEmail?: string;
  lineItemCount: number;
  subtotal: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
  currencyCode: string;
  dueDate: string;
  status: string;
  notes?: string;
  batchId?: string;
  createdAt: string;
}
