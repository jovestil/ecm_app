import { Injectable } from '@angular/core';

export interface NewHireEmailData {
  startDate: string;
  newEmployeeName: string;
  company: string;
  division: string;
  position: string;
  salaryCode: string;
  requestCreatedBy: string;
  employmentStatus: string;
  byod: string;
  rehire: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmailTemplateService {

  constructor() { }

  /**
   * Generate professional HTML email template for New Hire request
   * Uses inline CSS for email client compatibility
   */
  generateNewHireEmailHtml(data: NewHireEmailData): string {
    return `
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>New Hire Request Confirmation</title>
</head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;">
  <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f5f5f5; padding: 20px 0;">
    <tr>
      <td align="center">
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600" style="background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">

          <!-- Header -->
          <tr>
            <td style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px 40px; text-align: center;">
              <h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 600;">New Hire Request</h1>
              <p style="margin: 10px 0 0 0; color: #f0f0f0; font-size: 14px;">Employee Change Management System</p>
            </td>
          </tr>

          <!-- Introduction -->
          <tr>
            <td style="padding: 30px 40px 20px 40px;">
              <p style="margin: 0; color: #333333; font-size: 16px; line-height: 1.6;">
                A new hire request has been submitted with the following information:
              </p>
            </td>
          </tr>

          <!-- Information Table -->
          <tr>
            <td style="padding: 0 40px 30px 40px;">
              <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="border-collapse: collapse;">

                <!-- Start Date -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151; width: 45%;">
                    Start Date (First Day of Employment)
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.startDate)}
                  </td>
                </tr>

                <!-- New Employee -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    New Employee
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.newEmployeeName)}
                  </td>
                </tr>

                <!-- Company -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Company
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.company)}
                  </td>
                </tr>

                <!-- Division -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Division
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.division)}
                  </td>
                </tr>

                <!-- Position -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Position
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.position)}
                  </td>
                </tr>

                <!-- Hourly/Salaried -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Hourly/Salaried
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.salaryCode)}
                  </td>
                </tr>

                <!-- Request Created By -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Request Created By
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.requestCreatedBy)}
                  </td>
                </tr>

                <!-- Employment Status -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Employment Status
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.employmentStatus)}
                  </td>
                </tr>

                <!-- BYOD -->
                <tr>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    BYOD (Bring Your Own Device)
                  </td>
                  <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #1f2937;">
                    ${this.escapeHtml(data.byod)}
                  </td>
                </tr>

                <!-- Rehire -->
                <tr>
                  <td style="padding: 12px 16px; background-color: #f9fafb; font-weight: 600; color: #374151;">
                    Rehire
                  </td>
                  <td style="padding: 12px 16px; color: #1f2937;">
                    ${this.escapeHtml(data.rehire)}
                  </td>
                </tr>

              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style="padding: 20px 40px 30px 40px; background-color: #f9fafb; border-top: 1px solid #e5e7eb;">
              <p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px; line-height: 1.6;">
                This is an automated notification from the Employee Change Management System.
              </p>
              <p style="margin: 0; color: #6b7280; font-size: 13px;">
                <strong>Timestamp:</strong> ${new Date().toLocaleString()}
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
    `.trim();
  }

  /**
   * Escape HTML special characters to prevent XSS
   */
  private escapeHtml(text: string): string {
    const map: { [key: string]: string } = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, (m) => map[m]);
  }

  /**
   * Generate plain text version of the email for fallback
   */
  generateNewHireEmailPlainText(data: NewHireEmailData): string {
    return `
NEW HIRE REQUEST CONFIRMATION
==============================

A new hire request has been submitted with the following information:

Start Date (First Day of Employment): ${data.startDate}
New Employee: ${data.newEmployeeName}
Company: ${data.company}
Division: ${data.division}
Position: ${data.position}
Hourly/Salaried: ${data.salaryCode}
Request Created By: ${data.requestCreatedBy}
Employment Status: ${data.employmentStatus}
BYOD (Bring Your Own Device): ${data.byod}
Rehire: ${data.rehire}

---
This is an automated notification from the Employee Change Management System.
Timestamp: ${new Date().toLocaleString()}
    `.trim();
  }
}
