import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
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
    MatIconModule
  ]
})
export class SearchComponent {

  query: string = '';
  words: any[] = [];
  displayedColumns: string[] = ['word', 'occurrence', 'files'];
  errorMessage: string = '';

  constructor(private http: HttpClient) {}

  searchWords() {
    if (!this.query) {
      this.errorMessage = 'Please enter a search keyword.';
      return;
    }

    this.errorMessage = '';
    console.log(`Searching words: ${environment.apiUrl}/search/query?query=${this.query}`);

    this.http.get<any[]>(`${environment.apiUrl}/search/query?query=${this.query}`)
      .subscribe({
        next: (response) => {
          console.log('Response:', response);
          this.words = Array.isArray(response) ? response : [];

          this.words.forEach(word => word.visibleFiles = 5);

          if (this.words.length === 0) {
            this.errorMessage = 'No matching words found.';
          }
        },
        error: (error) => {
          console.error('Error searching words:', error);
          this.words = [];
          this.errorMessage = 'An error occurred while searching. Please try again.';
        }
      });
  }

  showMore(word: any) {
    word.visibleFiles = Math.min(word.visibleFiles + 5, word.files.length);
  }

  showLess(word: any) {
    word.visibleFiles = Math.max(word.visibleFiles - 5, 5);
  }

  downloadFile(fileId: number, fileName: string) {
    console.log(`Downloading file: ${fileName}`);

    this.http.get(`${environment.apiUrl}/files/download/${fileId}`, {
      responseType: 'blob'
    }).subscribe({
      next: (blob) => {
        const a = document.createElement('a');
        const objectUrl = URL.createObjectURL(blob);
        a.href = objectUrl;

        a.download = fileName.endsWith('.txt') ? fileName : `${fileName}.txt`;

        a.click();
        URL.revokeObjectURL(objectUrl);
      },
      error: (error) => {
        console.error('Error downloading file:', error);
        this.errorMessage = 'Error downloading file. Please try again.';
      }
    });
  }



  // downloadFile(fileId: number, fileName: string) {
  //   console.log(`Downloading file: ${fileName}`);
  //
  //   this.http.get(`${environment.apiUrl}/files/download/${fileId}`, {
  //     responseType: 'blob'
  //   }).subscribe({
  //     next: (blob) => {
  //       const a = document.createElement('a');
  //       const objectUrl = URL.createObjectURL(blob);
  //       a.href = objectUrl;
  //       a.download = fileName;
  //       a.click();
  //       URL.revokeObjectURL(objectUrl);
  //     },
  //     error: (error) => {
  //       console.error('Error downloading file:', error);
  //       this.errorMessage = 'Error downloading file. Please try again.';
  //     }
  //   });
  // }
}
