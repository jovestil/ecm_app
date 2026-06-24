import { Component, Input } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-back-to-homepage-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './back-to-homepage-button.component.html',
  styleUrls: ['./back-to-homepage-button.component.css']
})
export class BackToHomepageButtonComponent {
  @Input() showButton: boolean = false;
  @Input() parentId: number | null = null;

  constructor(private router: Router, private location: Location) {}

  goBack(): void {
    this.location.back();
  }
}