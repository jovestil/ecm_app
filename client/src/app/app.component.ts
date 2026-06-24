import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { EventMessage, EventType, InteractionStatus } from '@azure/msal-browser';
import { environment } from '../environments/environment';
import { AuthService } from './core/services/auth.service';
import { ToasterComponent } from './shared/toaster/toaster.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToasterComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'mathy-elm-client';
  private readonly _destroying$ = new Subject<void>();

  constructor(
    private msalService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Log developer name from environment variables
    if (environment.devname) {
      console.log(`🚀 Angular app started by: ${environment.devname}`);
      console.log(`📋 Environment: ${environment.configuration}`);
      console.log(`🔗 API URL: ${environment.apiUrl}`);
    }

    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.checkAndSetActiveAccount();
      });

    // Listen for login success events to check authorization
    this.msalBroadcastService.msalSubject$
      .pipe(
        filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
        takeUntil(this._destroying$)
      )
      .subscribe((result: EventMessage) => {
        console.log('Login successful:', result);
        this.checkUserAuthorization();
      });
  }

  ngOnDestroy(): void {
    this._destroying$.next();
    this._destroying$.complete();
  }

  private checkAndSetActiveAccount(): void {
    const activeAccount = this.msalService.instance.getActiveAccount();

    if (!activeAccount && this.msalService.instance.getAllAccounts().length > 0) {
      const accounts = this.msalService.instance.getAllAccounts();
      this.msalService.instance.setActiveAccount(accounts[0]);
    }
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      this.router.navigate(['/unauthorized']);
    }
  }
}
