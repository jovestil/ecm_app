import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(
    private msalService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean> {
    return this.msalBroadcastService.inProgress$.pipe(
      map((status: InteractionStatus) => {
        if (status === InteractionStatus.None) {
          const activeAccount = this.msalService.instance.getActiveAccount();
          console.log(123123);
          if (activeAccount) {
            return true;
          } else {
            // Redirect to login if not authenticated
            this.msalService.loginRedirect({
              scopes: environment.msal.scopes
            });
            return false;
          }
        }
        return false;
      })
    );
  }
}