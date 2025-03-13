import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { SlicePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-search',
  standalone: true,
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    SlicePipe,
    MatIconModule,
    SlicePipe
  ]
})
export class SearchComponent {
  query: string = '';
  emails: any[] = [];
  displayedColumns: string[] = ['fileName', 'content'];

  constructor(private http: HttpClient) {}

  searchEmails() {
    if (!this.query) return;

    this.http.get<any>(`http://localhost:5000/api/search/query?query=${this.query}`).subscribe({
      next: (response) => {
        this.emails = response.results;
      },
      error: (error) => {
        console.error('Error searching emails:', error);
      }
    });
  }

  downloadEmail(email: any) {
    const fileName = email.file_name || 'email.txt';
    const fileContent = `File Name: ${email.file_name}\n\nContent:\n${email.content}`;

    const blob = new Blob([fileContent], { type: 'text/plain' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(a.href);
  }

}
