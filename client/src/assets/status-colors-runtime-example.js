/* 
 * Example: How to change status colors at runtime using JavaScript
 * 
 * This can be executed in browser console, injected via external script,
 * or added to the application initialization
 */

// Method 1: Direct CSS custom property manipulation
function changeStatusColors() {
  const root = document.documentElement;
  
  // Change Pending to blue theme
  root.style.setProperty('--status-pending-bg', '#dbeafe');
  root.style.setProperty('--status-pending-text', '#1e40af');
  
  // Change Processing to purple theme  
  root.style.setProperty('--status-processing-bg', '#e9d5ff');
  root.style.setProperty('--status-processing-text', '#7c2d12');
  
  // Change Completed to darker green
  root.style.setProperty('--status-completed-bg', '#bbf7d0');
  root.style.setProperty('--status-completed-text', '#14532d');
  
  // Change Failed to darker red
  root.style.setProperty('--status-failed-bg', '#fecaca');
  root.style.setProperty('--status-failed-text', '#7f1d1d');
  
  // Change Cancelled to different orange
  root.style.setProperty('--status-cancelled-bg', '#ffedd5');
  root.style.setProperty('--status-cancelled-text', '#ea580c');
}

// Method 2: Using the StatusColorHelper utility (if available in global scope)
function changeStatusColorsWithHelper() {
  // This assumes StatusColorHelper is available globally
  if (window.StatusColorHelper) {
    window.StatusColorHelper.updateStatusColors({
      'Pending': { bg: '#dbeafe', text: '#1e40af' },
      'Processing': { bg: '#e9d5ff', text: '#7c2d12' },
      'Completed': { bg: '#bbf7d0', text: '#14532d' },
      'Failed': { bg: '#fecaca', text: '#7f1d1d' },
      'Cancelled': { bg: '#ffedd5', text: '#ea580c' }
    });
  }
}

// Method 3: Load colors from external JSON file
async function loadColorsFromConfig() {
  try {
    const response = await fetch('/assets/config/status-colors.json');
    const config = await response.json();
    
    const root = document.documentElement;
    Object.entries(config.statusColors).forEach(([status, colors]) => {
      const normalizedStatus = status.toLowerCase();
      root.style.setProperty(`--status-${normalizedStatus}-bg`, colors.bg);
      root.style.setProperty(`--status-${normalizedStatus}-text`, colors.text);
    });
    
    console.log('Status colors updated from config file');
  } catch (error) {
    console.error('Failed to load status colors config:', error);
  }
}

// Example usage:
// changeStatusColors();
// changeStatusColorsWithHelper(); 
// loadColorsFromConfig();

// To reset to defaults:
// document.documentElement.style.removeProperty('--status-pending-bg');
// document.documentElement.style.removeProperty('--status-pending-text');
// ... etc for all status types