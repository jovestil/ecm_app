import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

interface NavigationItem {
  title: string;
  description: string;
  route: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
  userName = 'John Doe';
  
  navigationItems: NavigationItem[] = [
    {
      title: 'HR Request Dashboard',
      description: 'View and manage all HR requests in one place',
      route: '/hr_request_dashboard',
      icon: '📊',
      color: '#3b82f6'
    },
    {
      title: 'New Hire Request',
      description: 'Submit a request to hire a new employee',
      route: '/new-hire',
      icon: '👤',
      color: '#059669'
    },
    {
      title: 'Promotion Request',
      description: 'Request employee promotion or transfer',
      route: '/promotion',
      icon: '⬆️',
      color: '#7c3aed'
    },
    {
      title: 'Layoff Request',
      description: 'Submit employee layoff requests',
      route: '/layoff',
      icon: '📋',
      color: '#991b1b'
    },
    {
      title: 'Return to Work',
      description: 'Process employee return after layoff',
      route: '/return-to-work',
      icon: '🔄',
      color: '#0891b2'
    },
    {
      title: 'Termination Request',
      description: 'Request employee termination',
      route: '/termination',
      icon: '❌',
      color: '#dc2626'
    }
  ];

  constructor(private router: Router) {}

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  handleLogout(): void {
    if (confirm('Are you sure you want to log out?')) {
      this.router.navigate(['/login']);
    }
  }
}