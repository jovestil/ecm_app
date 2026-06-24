import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-logout',
  standalone: true,
  template: `
    <div class="logout-container">
      <div class="logout-message">
        <h2>Logging you out...</h2>
        <p>Please wait while we securely log you out of the system.</p>
      </div>
    </div>
  `,
  styles: [`
    .logout-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
    
    .logout-message {
      background: white;
      padding: 40px;
      border-radius: 12px;
      text-align: center;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
      max-width: 400px;
    }
    
    .logout-message h2 {
      color: #1f2937;
      margin-bottom: 16px;
      font-size: 24px;
    }
    
    .logout-message p {
      color: #6b7280;
      margin: 0;
      font-size: 16px;
    }
  `]
})
export class LogoutComponent implements OnInit {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    try {
      // Wait for MSAL to be properly initialized
      await this.authService.initialize();
      this.authService.logout();
    } catch (error) {
      console.error('Error during logout:', error);
      // If logout fails, still redirect to root
      this.router.navigate(['/']);
    }
  }
}