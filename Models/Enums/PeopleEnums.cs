namespace HospitalManagement.Api.Models.Enums;

public enum Gender { Male, Female, Other }

public enum BloodGroup
{
    Unknown, APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative, OPositive, ONegative
}

public enum PatientStatus { Active, Inactive, Deceased }

public enum StaffStatus { Active, Inactive, OnLeave }

public enum NurseShift { Morning, Evening, Night }

public enum AllergySeverity { Mild, Moderate, Severe }
