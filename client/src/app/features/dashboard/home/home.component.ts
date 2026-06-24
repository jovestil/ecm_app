import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

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
export class HomeComponent implements OnInit {
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

  constructor(private router: Router, private authService: AuthService) {}

  async ngOnInit(): Promise<void> {
    await this.checkUserAuthorization();
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for home dashboard');
      this.router.navigate(['/unauthorized']);
    }
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  handleLogout(): void {
    if (confirm('Are you sure you want to log out?')) {
      this.router.navigate(['/login']);
    }
  }
}