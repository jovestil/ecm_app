import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './unauthorized.component.html',
  styleUrls: ['./unauthorized.component.css']
})
export class UnauthorizedComponent implements OnInit, OnDestroy {
  countdown = 3;
  private intervalId: any;
  userEmail: string = '';
  userRoles: string[] = [];

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.startCountdown();
  }

  ngOnDestroy(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  private startCountdown(): void {
    this.intervalId = setInterval(() => {
      this.countdown--;
      if (this.countdown <= 0) {
        this.logout();
      }
    }, 1000);
  }

  private logout(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
    this.router.navigate(['/logout']);
  }

  public logoutNow(): void {
    this.logout();
  }

  private async loadUserInfo(): Promise<void> {
    try {
      // Initialize MSAL first
      await this.authService.initialize();
      
      this.userEmail = this.authService.getUserEmail();
      this.userRoles = await this.authService.getUserRoles();
    } catch (error) {
      console.error('Error loading user info:', error);
      // Set defaults if initialization fails
      this.userEmail = 'Unknown';
      this.userRoles = [];
    }
  }
}