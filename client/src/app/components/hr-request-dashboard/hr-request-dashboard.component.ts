import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HRRequest, RequestType, RequestTypeOption } from '../../models/hr-request.model';

@Component({
  selector: 'app-hr-request-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './hr-request-dashboard.component.html',
  styleUrls: ['./hr-request-dashboard.component.css']
})
export class HrRequestDashboardComponent implements OnInit {
  requests: HRRequest[] = [];
  filteredRequests: HRRequest[] = [];
  searchTerm = '';
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  showModal = false;
  userName = 'John Doe';

  requestTypeOptions: RequestTypeOption[] = [
    {
      type: 'new-hire',
      title: 'New Hire',
      description: 'Request to hire a new employee',
      icon: 'NH',
      route: '/new-hire'
    },
    {
      type: 'promotion',
      title: 'Promotion / Transfer',
      description: 'Request to promote or transfer an employee',
      icon: 'PT',
      route: '/promotion'
    },
    {
      type: 'layoff',
      title: 'Layoff',
      description: 'Request for employee layoff',
      icon: 'L',
      route: '/layoff'
    },
    {
      type: 'return',
      title: 'Return to Work',
      description: 'Request for employee return after layoff',
      icon: 'R',
      route: '/return-to-work'
    },
    {
      type: 'termination',
      title: 'Termination',
      description: 'Request to terminate an employee',
      icon: 'T',
      route: '/termination'
    }
  ];

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.initializeData();
  }

  initializeData(): void {
    this.requests = [
      {
        id: '1',
        type: 'new-hire',
        employeeName: 'Sarah Johnson',
        effectiveDate: 'June 15, 2025',
        status: 'submitted',
        submittedBy: 'Michael Chen'
      },
      {
        id: '2',
        type: 'promotion',
        employeeName: 'David Rodriguez',
        effectiveDate: 'July 1, 2025',
        status: 'draft',
        submittedBy: 'Lisa Wang'
      },
      {
        id: '3',
        type: 'transfer',
        employeeName: 'Emma Thompson',
        effectiveDate: 'June 30, 2025',
        status: 'submitted',
        submittedBy: 'Michael Chen'
      },
      {
        id: '4',
        type: 'termination',
        employeeName: 'James Wilson',
        effectiveDate: 'June 10, 2025',
        status: 'draft',
        submittedBy: 'Jennifer Lee'
      },
      {
        id: '5',
        type: 'return',
        employeeName: 'Maria Garcia',
        effectiveDate: 'June 20, 2025',
        status: 'submitted',
        submittedBy: 'Robert Kim'
      },
      {
        id: '6',
        type: 'layoff',
        employeeName: 'Alex Turner',
        effectiveDate: 'May 31, 2025',
        status: 'draft',
        submittedBy: 'Michael Chen'
      }
    ];
    this.filteredRequests = [...this.requests];
    this.updatePagination();
  }

  onSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm = target.value.toLowerCase();
    this.filterRequests();
  }

  filterRequests(): void {
    this.filteredRequests = this.requests.filter(request =>
      Object.values(request).some(value =>
        value?.toString().toLowerCase().includes(this.searchTerm)
      )
    );
    this.currentPage = 1;
    this.updatePagination();
  }

  changePage(delta: number): void {
    const newPage = this.currentPage + delta;
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.currentPage = newPage;
    }
  }

  updatePageSize(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.pageSize = parseInt(target.value);
    this.currentPage = 1;
    this.updatePagination();
  }

  updatePagination(): void {
    this.totalPages = Math.ceil(this.filteredRequests.length / this.pageSize);
  }

  get paginatedRequests(): HRRequest[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.filteredRequests.slice(startIndex, endIndex);
  }

  get canGoPrevious(): boolean {
    return this.currentPage > 1;
  }

  get canGoNext(): boolean {
    return this.currentPage < this.totalPages;
  }

  openModal(): void {
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  createRequest(type: RequestType): void {
    this.closeModal();
    const option = this.requestTypeOptions.find(opt => opt.type === type);
    if (option) {
      this.router.navigate([option.route]);
    }
  }

  viewRequest(request: HRRequest): void {
    console.log('Viewing request:', request);
  }

  formatRequestType(type: RequestType): string {
    return type.split('-').map(word => 
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
  }

  formatStatus(status: string): string {
    return status.split('-').map(word => 
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
  }

  handleLogout(): void {
    if (confirm('Are you sure you want to log out?')) {
      this.router.navigate(['/login']);
    }
  }
}