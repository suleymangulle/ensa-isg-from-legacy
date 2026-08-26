// ---------------------------------------------------------------------------
// GENERATED FILE - do not edit by hand.
//
// Mirrors src/Ensa.Application.Contracts/Permissions/EnsaPermissions.cs. These strings are
// both the `ensa:permission` claim values in the token and the authorization policy names
// on the server, so a copy that drifts by one character silently hides a screen from
// someone entitled to it. Regenerate with:
//
//     python tools/gen-enums/gen_permissions.py
//
// Hiding a link is a courtesy, never a control: every endpoint enforces its own
// permission and answers 403 regardless of what the interface shows.
// ---------------------------------------------------------------------------

export const PERMISSIONS = {
  /** Organization (tenant) management — host administrators only. */
  Tenant: {
    Default: 'Ensa.Tenant',
    Create: 'Ensa.Tenant.Create',
    Update: 'Ensa.Tenant.Update',
    Delete: 'Ensa.Tenant.Delete',
  },
  Office: {
    Default: 'Ensa.Office',
    Create: 'Ensa.Office.Create',
    Update: 'Ensa.Office.Update',
    Delete: 'Ensa.Office.Delete',
  },
  User: {
    Default: 'Ensa.User',
    Create: 'Ensa.User.Create',
    Update: 'Ensa.User.Update',
    Delete: 'Ensa.User.Delete',
    Export: 'Ensa.User.Export',
  },
  Role: {
    Default: 'Ensa.Role',
    Create: 'Ensa.Role.Create',
    Update: 'Ensa.Role.Update',
    Delete: 'Ensa.Role.Delete',
  },
  /** Permission definitions and user/role permission assignments. */
  Permission: {
    Default: 'Ensa.Permission',
    Create: 'Ensa.Permission.Create',
    Update: 'Ensa.Permission.Update',
    Delete: 'Ensa.Permission.Delete',
  },
  Menu: {
    Default: 'Ensa.Menu',
    Create: 'Ensa.Menu.Create',
    Update: 'Ensa.Menu.Update',
    Delete: 'Ensa.Menu.Delete',
  },
  Company: {
    Default: 'Ensa.Company',
    Create: 'Ensa.Company.Create',
    Update: 'Ensa.Company.Update',
    Delete: 'Ensa.Company.Delete',
    Export: 'Ensa.Company.Export',
    Approve: 'Ensa.Company.Approve',
  },
  CompanyEmployee: {
    Default: 'Ensa.CompanyEmployee',
    Create: 'Ensa.CompanyEmployee.Create',
    Update: 'Ensa.CompanyEmployee.Update',
    Delete: 'Ensa.CompanyEmployee.Delete',
    Export: 'Ensa.CompanyEmployee.Export',
  },
  WorkplaceDepartment: {
    Default: 'Ensa.WorkplaceDepartment',
    Create: 'Ensa.WorkplaceDepartment.Create',
    Update: 'Ensa.WorkplaceDepartment.Update',
    Delete: 'Ensa.WorkplaceDepartment.Delete',
  },
  Document: {
    Default: 'Ensa.Document',
    Create: 'Ensa.Document.Create',
    Update: 'Ensa.Document.Update',
    Delete: 'Ensa.Document.Delete',
  },
  Form: {
    Default: 'Ensa.Form',
    Create: 'Ensa.Form.Create',
    Update: 'Ensa.Form.Update',
    Delete: 'Ensa.Form.Delete',
    Export: 'Ensa.Form.Export',
  },
  Training: {
    Default: 'Ensa.Training',
    Create: 'Ensa.Training.Create',
    Update: 'Ensa.Training.Update',
    Delete: 'Ensa.Training.Delete',
    Export: 'Ensa.Training.Export',
  },
  TrainingPlan: {
    Default: 'Ensa.TrainingPlan',
    Create: 'Ensa.TrainingPlan.Create',
    Update: 'Ensa.TrainingPlan.Update',
    Delete: 'Ensa.TrainingPlan.Delete',
    Export: 'Ensa.TrainingPlan.Export',
    Approve: 'Ensa.TrainingPlan.Approve',
  },
  WorkPlan: {
    Default: 'Ensa.WorkPlan',
    Create: 'Ensa.WorkPlan.Create',
    Update: 'Ensa.WorkPlan.Update',
    Delete: 'Ensa.WorkPlan.Delete',
    Export: 'Ensa.WorkPlan.Export',
    Approve: 'Ensa.WorkPlan.Approve',
  },
  Activity: {
    Default: 'Ensa.Activity',
    Create: 'Ensa.Activity.Create',
    Update: 'Ensa.Activity.Update',
    Delete: 'Ensa.Activity.Delete',
    Export: 'Ensa.Activity.Export',
    Approve: 'Ensa.Activity.Approve',
  },
  RiskAssessment: {
    Default: 'Ensa.RiskAssessment',
    Create: 'Ensa.RiskAssessment.Create',
    Update: 'Ensa.RiskAssessment.Update',
    Delete: 'Ensa.RiskAssessment.Delete',
    Export: 'Ensa.RiskAssessment.Export',
    Approve: 'Ensa.RiskAssessment.Approve',
  },
  /** Corrective and preventive action. */
  CorrectiveAction: {
    Default: 'Ensa.CorrectiveAction',
    Create: 'Ensa.CorrectiveAction.Create',
    Update: 'Ensa.CorrectiveAction.Update',
    Delete: 'Ensa.CorrectiveAction.Delete',
    Export: 'Ensa.CorrectiveAction.Export',
    Approve: 'Ensa.CorrectiveAction.Approve',
  },
  FieldObservation: {
    Default: 'Ensa.FieldObservation',
    Create: 'Ensa.FieldObservation.Create',
    Update: 'Ensa.FieldObservation.Update',
    Delete: 'Ensa.FieldObservation.Delete',
    Export: 'Ensa.FieldObservation.Export',
    Approve: 'Ensa.FieldObservation.Approve',
  },
  /** Workplace accidents and near-miss incidents. */
  Incident: {
    Default: 'Ensa.Incident',
    Create: 'Ensa.Incident.Create',
    Update: 'Ensa.Incident.Update',
    Delete: 'Ensa.Incident.Delete',
    Export: 'Ensa.Incident.Export',
    Approve: 'Ensa.Incident.Approve',
  },
  EmergencyPlan: {
    Default: 'Ensa.EmergencyPlan',
    Create: 'Ensa.EmergencyPlan.Create',
    Update: 'Ensa.EmergencyPlan.Update',
    Delete: 'Ensa.EmergencyPlan.Delete',
    Export: 'Ensa.EmergencyPlan.Export',
    Approve: 'Ensa.EmergencyPlan.Approve',
  },
  Equipment: {
    Default: 'Ensa.Equipment',
    Create: 'Ensa.Equipment.Create',
    Update: 'Ensa.Equipment.Update',
    Delete: 'Ensa.Equipment.Delete',
    Export: 'Ensa.Equipment.Export',
  },
  MedicalExamination: {
    Default: 'Ensa.MedicalExamination',
    Create: 'Ensa.MedicalExamination.Create',
    Update: 'Ensa.MedicalExamination.Update',
    Delete: 'Ensa.MedicalExamination.Delete',
    Export: 'Ensa.MedicalExamination.Export',
    Approve: 'Ensa.MedicalExamination.Approve',
  },
  EPrescription: {
    Default: 'Ensa.EPrescription',
    Create: 'Ensa.EPrescription.Create',
    Update: 'Ensa.EPrescription.Update',
    Delete: 'Ensa.EPrescription.Delete',
    Export: 'Ensa.EPrescription.Export',
  },
  /** ISG-KATIP / IBYS integration. */
  Ibys: {
    Default: 'Ensa.Ibys',
    Create: 'Ensa.Ibys.Create',
    Update: 'Ensa.Ibys.Update',
    Delete: 'Ensa.Ibys.Delete',
    Export: 'Ensa.Ibys.Export',
    Approve: 'Ensa.Ibys.Approve',
  },
  Invoice: {
    Default: 'Ensa.Invoice',
    Create: 'Ensa.Invoice.Create',
    Update: 'Ensa.Invoice.Update',
    Delete: 'Ensa.Invoice.Delete',
    Export: 'Ensa.Invoice.Export',
    Approve: 'Ensa.Invoice.Approve',
  },
  CashRegister: {
    Default: 'Ensa.CashRegister',
    Create: 'Ensa.CashRegister.Create',
    Update: 'Ensa.CashRegister.Update',
    Delete: 'Ensa.CashRegister.Delete',
    Export: 'Ensa.CashRegister.Export',
  },
  /** Administrative fine definitions and records. */
  Penalty: {
    Default: 'Ensa.Penalty',
    Create: 'Ensa.Penalty.Create',
    Update: 'Ensa.Penalty.Update',
    Delete: 'Ensa.Penalty.Delete',
    Export: 'Ensa.Penalty.Export',
  },
  Visit: {
    Default: 'Ensa.Visit',
    Create: 'Ensa.Visit.Create',
    Update: 'Ensa.Visit.Update',
    Delete: 'Ensa.Visit.Delete',
    Export: 'Ensa.Visit.Export',
    Approve: 'Ensa.Visit.Approve',
  },
  Mail: {
    Default: 'Ensa.Mail',
    Create: 'Ensa.Mail.Create',
    Update: 'Ensa.Mail.Update',
    Delete: 'Ensa.Mail.Delete',
    Export: 'Ensa.Mail.Export',
  },
  Message: {
    Default: 'Ensa.Message',
    Create: 'Ensa.Message.Create',
    Update: 'Ensa.Message.Update',
    Delete: 'Ensa.Message.Delete',
  },
  SupportTicket: {
    Default: 'Ensa.SupportTicket',
    Create: 'Ensa.SupportTicket.Create',
    Update: 'Ensa.SupportTicket.Update',
    Delete: 'Ensa.SupportTicket.Delete',
    Export: 'Ensa.SupportTicket.Export',
    Approve: 'Ensa.SupportTicket.Approve',
  },
  Report: {
    Default: 'Ensa.Report',
    Create: 'Ensa.Report.Create',
    Update: 'Ensa.Report.Update',
    Delete: 'Ensa.Report.Delete',
    Export: 'Ensa.Report.Export',
  },
  /** Shared reference tables (city, occupation code, period, hazard and so on). */
  Lookups: {
    Default: 'Ensa.Lookups',
    Create: 'Ensa.Lookups.Create',
    Update: 'Ensa.Lookups.Update',
    Delete: 'Ensa.Lookups.Delete',
    Export: 'Ensa.Lookups.Export',
  },
} as const
