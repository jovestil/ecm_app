import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

export const routes: Routes = [
  {
    path: 'unauthorized',
    loadComponent: () => import('./features/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  {
    path: 'logout',
    loadComponent: () => import('./features/logout/logout.component').then(m => m.LogoutComponent)
  },
  {
    path: 'home',
    loadComponent: () => import('./features/dashboard/home/home.component').then(m => m.HomeComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'me',
    loadComponent: () => import('./features/dashboard/me/me.component').then(m => m.MeComponent),
    canActivate: [MsalGuard]
  },
  {
    path: '',
    loadComponent: () => import('./features/dashboard/hr-request-dashboard/hr-request-dashboard.component').then(m => m.HrRequestDashboardComponent),
    pathMatch: 'full',
    canActivate: [MsalGuard]
  },
  {
    path: 'layoff',
    loadComponent: () => import('./features/hr-requests/layoff-request/layoff-request.component').then(m => m.LayoffRequestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'promotion',
    loadComponent: () => import('./features/hr-requests/promotion-request/promotion-request.component').then(m => m.PromotionRequestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'promotion/view/:parentId',
    loadComponent: () => import('./features/hr-requests/promotion-request/promotion-request.component').then(m => m.PromotionRequestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'return-to-work',
    loadComponent: () => import('./features/hr-requests/return-to-work/return-to-work.component').then(m => m.ReturnToWorkComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'termination',
    loadComponent: () => import('./features/hr-requests/termination-request/termination-request.component').then(m => m.TerminationRequestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'new-hire',
    loadComponent: () => import('./features/hr-requests/new-hire-request/new-hire-request.component').then(m => m.NewHireRequestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'new-hire/view/:parentId',
    loadComponent: () => import('./features/hr-requests/new-hire-request/new-hire-request.component').then(m => m.NewHireRequestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'viewpointsync',
    loadComponent: () => import('./features/admin/viewpoint-sync/viewpoint-sync.component').then(m => m.ViewpointSyncComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'ad-user-test',
    loadComponent: () => import('./features/admin/ad-user-test/ad-user-test.component').then(m => m.AdUserTestComponent),
    canActivate: [MsalGuard]
  },
  {
    path: 'email-test',
    loadComponent: () => import('./features/admin/email-test/email-test.component').then(m => m.EmailTestComponent),
    canActivate: [MsalGuard]
  }
];
