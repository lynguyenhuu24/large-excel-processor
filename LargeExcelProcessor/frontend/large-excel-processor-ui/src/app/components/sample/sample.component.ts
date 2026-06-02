import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExcelService } from '../../services/excel.service';

@Component({
  selector: 'app-sample',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sample.component.html',
  styleUrl: './sample.component.scss',
})
export class SampleComponent {
  count = 100;
  generating = false;
  error: string | null = null;

  constructor(private excelService: ExcelService) {}

  generate(): void {
    const c = Math.max(1, Math.min(10_000, this.count || 1));
    this.count = c;
    this.generating = true;
    this.error = null;

    this.excelService.downloadSample(c).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'sample-invoices.xlsx';
        a.click();
        URL.revokeObjectURL(url);
        this.generating = false;
      },
      error: (err) => {
        this.error = err.message ?? 'Failed to generate sample file.';
        this.generating = false;
      },
    });
  }
}
