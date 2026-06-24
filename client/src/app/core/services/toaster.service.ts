import { Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';

/**
 * ToasterService provides a centralized way to display toast notifications
 * throughout the application using PrimeNG's toast component.
 * 
 * Usage example:
 * ```typescript
 * constructor(private toasterService: ToasterService) {}
 * 
 * // Show success message
 * this.toasterService.showSuccess('Data saved successfully!');
 * 
 * // Show error with custom title
 * this.toasterService.showError('Failed to save data', 'Save Error');
 * 
 * // Show warning with custom duration (10 seconds)
 * this.toasterService.showWarning('Please review your input', 'Validation Warning', 10000);
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class ToasterService {
  private readonly defaultLife = 5000; // 5 seconds
  private readonly defaultPosition = 'top-right';

  constructor(private messageService: MessageService) {}

  /**
   * Show success message
   * @param message The message content
   * @param title Optional title (defaults to 'Success')
   * @param life Optional duration in ms (defaults to 5000)
   */
  showSuccess(message: string, title?: string, life?: number): void {
    this.messageService.add({
      severity: 'success',
      summary: title || 'Success',
      detail: message,
      life: life || this.defaultLife
    });
  }

  /**
   * Show error message
   * @param message The message content
   * @param title Optional title (defaults to 'Error')
   * @param life Optional duration in ms (defaults to 5000)
   */
  showError(message: string, title?: string, life?: number): void {
    this.messageService.add({
      severity: 'error',
      summary: title || 'Error',
      detail: message,
      life: life || this.defaultLife
    });
  }

  /**
   * Show warning message
   * @param message The message content
   * @param title Optional title (defaults to 'Warning')
   * @param life Optional duration in ms (defaults to 5000)
   */
  showWarning(message: string, title?: string, life?: number): void {
    this.messageService.add({
      severity: 'warn',
      summary: title || 'Warning',
      detail: message,
      life: life || this.defaultLife
    });
  }

  /**
   * Show info message
   * @param message The message content
   * @param title Optional title (defaults to 'Info')
   * @param life Optional duration in ms (defaults to 5000)
   */
  showInfo(message: string, title?: string, life?: number): void {
    this.messageService.add({
      severity: 'info',
      summary: title || 'Info',
      detail: message,
      life: life || this.defaultLife
    });
  }

  /**
   * Clear all messages
   */
  clear(): void {
    this.messageService.clear();
  }

  /**
   * Clear a specific message by key
   * @param key The message key
   */
  clearByKey(key: string): void {
    this.messageService.clear(key);
  }
}