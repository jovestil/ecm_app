-- Migration script to fix EmployeeDeparmentCode field
-- 1. Rename column from EmployeeDeparmentCode to EmployeeDepartmentCode
-- 2. Change data type from int to nvarchar(10) to match department codes

-- First, add the new column with correct name and type
ALTER TABLE HRRequestDetails 
ADD EmployeeDepartmentCode nvarchar(10) NULL;

-- Copy data from old column to new column, converting int to string
UPDATE HRRequestDetails 
SET EmployeeDepartmentCode = CASE 
    WHEN EmployeeDeparmentCode IS NOT NULL 
    THEN CAST(EmployeeDeparmentCode AS nvarchar(10))
    ELSE NULL 
END;

-- Drop the old column
ALTER TABLE HRRequestDetails 
DROP COLUMN EmployeeDeparmentCode;