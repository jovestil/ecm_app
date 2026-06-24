// Status name to CSS class mapping for backend RequestStatus table
export const HR_REQUEST_STATUS_CLASSES: { [statusName: string]: string } = {
  'Pending': 'status-pending',
  'Processing': 'status-processing', 
  'Completed': 'status-completed',
  'Failed': 'status-failed',
  'Cancelled': 'status-cancelled'
};

export class StatusColorHelper {
  /**
   * Get CSS class name for a status by name
   * Colors are now controlled by CSS custom properties in common.css
   */
  static getStatusClassName(statusName: string): string {
    // Normalize the status name to handle potential variations
    const normalizedName = statusName.trim();
    return HR_REQUEST_STATUS_CLASSES[normalizedName] || 'status-default';
  }

  /**
   * Get CSS class name for display status based on both display status and actual status
   * Implements custom color logic:
   * - If RequestDisplayStatusName == 'Submitted' and RequestStatusName == 'Pending'|'Processing'|'Completed' → Green
   * - If RequestDisplayStatusName == 'Submitted' and RequestStatusName == 'Failed' → Red
   * - If RequestDisplayStatusName == 'Submitted' and RequestStatusName == 'Cancelled' → Orange
   * - If RequestDisplayStatusName == 'Draft' → Gray
   */
  static getDisplayStatusClassName(displayStatusName?: string, actualStatusName?: string): string {
    if (!displayStatusName) {
      return StatusColorHelper.getStatusClassName(actualStatusName || '');
    }

    const normalizedDisplayStatus = displayStatusName.trim();
    const normalizedActualStatus = actualStatusName?.trim() || '';

    if (normalizedDisplayStatus === 'Draft') {
      return 'status-draft';
    }

    if (normalizedDisplayStatus === 'Submitted') {
      if (['Pending', 'Processing', 'Completed'].includes(normalizedActualStatus)) {
        return 'status-submitted-success';
      } else if (normalizedActualStatus === 'Failed') {
        return 'status-submitted-failed';
      } else if (normalizedActualStatus === 'Cancelled') {
        return 'status-submitted-cancelled';
      }
    }

    // Fallback to original logic if no match
    return StatusColorHelper.getStatusClassName(actualStatusName || '');
  }

  /**
   * Dynamically change status colors at runtime by updating CSS custom properties
   * This allows changing colors after deployment without recompiling
   */
  static updateStatusColors(colors: { [statusName: string]: { bg: string; text: string } }): void {
    const root = document.documentElement;
    
    Object.entries(colors).forEach(([statusName, colorConfig]) => {
      const normalizedName = statusName.toLowerCase();
      root.style.setProperty(`--status-${normalizedName}-bg`, colorConfig.bg);
      root.style.setProperty(`--status-${normalizedName}-text`, colorConfig.text);
    });
  }

  /**
   * Reset status colors to default values defined in CSS
   */
  static resetStatusColors(): void {
    const root = document.documentElement;
    
    // Remove custom property overrides to fall back to CSS defaults
    Object.keys(HR_REQUEST_STATUS_CLASSES).forEach(statusName => {
      const normalizedName = statusName.toLowerCase();
      root.style.removeProperty(`--status-${normalizedName}-bg`);
      root.style.removeProperty(`--status-${normalizedName}-text`);
    });
  }
}