# FUTURE NEW HIRE 2.0

## PERSONAL INFO

| Field | Description |
|-------|-------------|
| **First/Last Name** | If Preferred First Name is empty, the First Name will be used for the AD account, email, and Viewpoint. |
| **Preferred First Name** | If populated, this value will be used for the AD account, email and Viewpoint; if left blank, it will use the data in the First Name data. |
| **Personal Email** | Used for: [To be specified] |
| **Personal Cell** | Used for: [To be specified] |
| **First Day of Employment** | Date selector |
| **Preferred Name** | Used in AD account, email address, and Viewpoint. |
| **Referred by** | Informational only - Data is sent in the general email that summarizes data entry to hiring manager. Does not trigger any action/process. |
| **Rehire** | Informational only – Data is sent in the general email that summarizes data entry to hiring manager. Does not trigger any action/process. |

## Position Information – Non-Union

| Field | Description | Notes |
|-------|-------------|-------|
| **Company** | Dropdown with all available hiring companies | How do we control the dropdown content? Split Energy/Mathy/Pavement – based on who is logging in and the user's group, show the appropriate Company options. Use Azure Groups – this will allow us to handle exceptions. |
| **Employment Status** | SP [dbo].[EmploymentStatus_List] | Company 19 will have Union statuses. |
| **Hourly/Salary** | Non-Union option only | SP [dbo].[HourlySalaried_ListGetbyCompanyID] |
| **Position** | SP [dbo].[Position_ListGetbyCompanyID] | |
| **Payroll Dept Code** | SP [dbo].[PayrollDeptCode_ListGetbyCompanyID] | |
| **Functional Department** | Need Use Case to bring back | Currently not on PVMT |
| **Time Off Supervisor** | Driven off Payroll Department Code to present the options | Non-Union option only. Currently not on PVMT. New data element in Vista? |
| **Physical Location** | SP [dbo].[PhysicalLocation_List] | |
| **Timecard Approver** | Driven off Payroll Department Code to present the options | **If Salary is selected as Employment Status, do not show. Currently not on PVMT. New data field in Vista |

## Position Information – Union

| Field | Description | Notes |
|-------|-------------|-------|
| **Company** | Dropdown with all available hiring companies | List will be driven by user's access security? SP [dbo].[Company_ListforDivGetbyID] |
| **Union** | **If the Company select is 19 - Mathy Construction Company, then present this option | |
| **Employment Status** | **If Union is selected to Yes, these are the available options. | SP [dbo].[EmploymentStatus_List] |
| **Position** | SP [dbo].[Position_ListGetbyCompanyID] | |
| **Payroll Dept code** | | |
| **Functional Department** | Waiting on Use Case | Currently not on PVMT |
| **Union Craft** | **If Union is selected to Yes, show this parameter | |
| **Union Wage** | **If Union is selected to Yes, show this parameter | |
| **Physical Location** | | |
| **Payroll Dept Code** | SP [dbo].[PayrollDeptCode_ListGetbyCompanyID] | |
| **Apprentice** | Yes / No | |
| **Apprentice Year** | Select years of Apprentice | **Shows when Apprentice is selected as Yes |

## To Be Removed

| Field | Description | Notes |
|-------|-------------|-------|
| **Timecard Method** | If Salary is selected as Employment Status, do not show | Currently not on PVMT |
| **Timecard Type** | **If Timecard Method is selected to Electronic, show this parameter. There is some logic built in right now. | Currently not on PVMT |
| **Timecard User Group** | Driven off Payroll Department Code to present the options **If Timecard Method is selected to Electronic, show this parameter | Currently not on PVMT |
| **Primary HR Representative** | | Currently not on PVMT |
| **Secondary HR Representative** | | Currently not on PVMT |

## Credit Cards

| Field | Description | Notes |
|-------|-------------|-------|
| **Kwik Trip Card** | Yes / No | |
| **Company Expense Card** | Yes / No | If yes, show the next three options: |
| **Fuel Only Credit Card** | Yes / No | |
| **EE Expense** | Yes / No | |
| **Weekly Limit** | | |
| **Fuel Cardlock Access** | Yes / No | |
| **Cardlock – ship address** | Populate with the Physical Location used above | **If Fuel Cardlock is selected as Yes |

## Company Vehicles Details

| Field | Description | Notes |
|-------|-------------|-------|
| **Approved to Operate?** | Yes / No | |
| **Classification** | **Show if Approved to Operate is Yes | |
| **D & A Profile** | **Show if Approved to Operate is Yes | |
| **Company Car** | Yes / No | **Show if Approved to Operate is Yes |
| **Application Part 2** | Yes / No | **Show if Approved to Operate is Yes |

## Building Access

- **Mathy/Energy**
- **Pavement**

## IT Related Information

### Non-Salary Employee
If the data entered specifies this is a "non-salary" employee, ask:

### Salary Employee
If the data entered specifies "salary" assumptions:
- Will need an email address
- Will need an E5 M$ license

### IT Hardware

**Mathy Tablet Profiles:**
**Pavement Tablet Profiles:**

**Add to UI:**
All items will be delivered to [Physical Location selected] unless noted here: [text box]

## Software

Pavement and Mathy/Energy will have two lists. Lists are stored in the Application Table in each for the New Hire DBs.