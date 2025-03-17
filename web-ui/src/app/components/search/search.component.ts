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
import { environment } from '../../environments/environment';



@Component({
  selector: 'app-search',
  standalone: true,
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css'],
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
  displayedColumns: string[] = ['fileName', 'content', 'download'];

  errorMessage: string = '';

  constructor(private http: HttpClient) {}

  searchEmails() {
    if (!this.query) {
      this.errorMessage = 'Please enter a search keyword.';
      return;
    }

    this.errorMessage = '';
    console.log(`Call from inside the component: ${environment.apiUrl}/search/query?query=${this.query}`);

    this.http.get<any[]>(`${environment.apiUrl}/search/query?query=${this.query}`)
      .subscribe({
        next: (response) => {
          console.log('Response:', response);
          this.emails = Array.isArray(response) ? response : [];

          const lowerQuery = this.query.toLowerCase();
          this.emails = this.emails.filter(email => email.content.toLowerCase().includes(lowerQuery));

          if (this.emails.length === 0) {
            this.errorMessage = 'No matching emails found.';
          }
        },
        error: (error) => {
          console.error('Error searching emails:', error);
          this.emails = [];
          this.errorMessage = 'An error occurred while searching. Please try again.';
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




  // searchEmails() {
  //   if (!this.query) return;
  //
  //   console.log(`Call from inside the component: ${environment.apiUrl}/search/query?query=${this.query}`);
  //
  //   this.http.get<any>(`${environment.apiUrl}/search/query?query=${this.query}`)
  //     .subscribe({
  //       next: (response) => {
  //         console.log('Response:', response?.results);
  //         // ✅ Ensure response.results is always an array
  //         this.emails = Array.isArray(response?.results) ? response.results : [];
  //       },
  //       error: (error) => {
  //         console.error('Error searching emails:', error);
  //         this.emails = []; // ✅ Prevents undefined errors
  //       }
  //     });
  // }


  // searchEmails() {
  //   if (!this.query) return;
  //   console.log(`Call from inside the component: ${environment.apiUrl}/search?query=${this.query}`);
  //   this.http.get<any>(`${environment.apiUrl}/search/query?query=${this.query}`)
  //
  //   //this.http.get<any>(`${environment.apiUrl}/search?query=${this.query}`)
  //
  //     .subscribe({
  //     next: (response) => {
  //       console.log('Response:', response.results);
  //       this.emails = response.results;
  //     },
  //     error: (error) => {
  //       console.error('Error searching emails:', error);
  //     }
  //   });
  // }

  // downloadEmail(email: any) {
  //   const fileName = email.file_name || 'email.txt';
  //   const fileContent = `File Name: ${email.file_name}\n\nContent:\n${email.content}`;
  //
  //   const blob = new Blob([fileContent], { type: 'text/plain' });
  //   const a = document.createElement('a');
  //   a.href = URL.createObjectURL(blob);
  //   a.download = fileName;
  //   a.click();
  //   URL.revokeObjectURL(a.href);
  // }

}
