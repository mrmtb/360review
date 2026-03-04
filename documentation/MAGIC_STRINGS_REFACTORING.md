# Magic Strings Refactoring Guide

This document identifies magic strings found throughout the codebase that should be replaced with constants from `AppConstants.cs`.

## ✅ Already in AppConstants

The following constants have been added to AppConstants:

### StatusNames (existing)
- `Draft`, `Self`, `ReminderGeneral`, `Completed`, `Unfamiliar`

### RoleNames (existing)
- `Admin`, `Client`, `Participant`, `AdminOrClient`, `AdminOrParticipant`

### EmailTemplateNames (NEW)
- `InvitationRater`, `InvitationRatee`, `ReminderGeneral`

### EvaluationParticipantStatusNames (NEW)
- All 8 status names from database

### EvaluationParticipantStatusIds (NEW)
- Integer constants 1-8 for status IDs

### ParticipantRoleNames (NEW)
- `Self`, `Manager`, `Peer`, `DirectReport`

### ErrorMessages (NEW)
- All common error messages used in controllers

### SuccessMessages (NEW)
- Common success messages

### ActionNames (NEW)
- Action method name strings used in RedirectToAction

### ViewDataKeys (NEW)
- ViewData dictionary keys

### PartialViewNames (NEW)
- Partial view name strings

---

## 🔧 Files Requiring Updates

### 1. EmailQueueHelper.cs (HIGH PRIORITY)

**Lines 152, 216**: Magic number `4` for "Invitation Sent" status
```csharp
// ❌ BEFORE
participant.EvaluationParticipantStatusId = 4; // Status for "Invitation Sent"

// ✅ AFTER
participant.EvaluationParticipantStatusId = EvaluationParticipantStatusIds.InvitationSent;
```

**Line 205**: Magic number `4` in query
```csharp
// ❌ BEFORE
&& ep.EvaluationParticipantStatusId == 4

// ✅ AFTER
&& ep.EvaluationParticipantStatusId == EvaluationParticipantStatusIds.InvitationSent
```

**Lines 28, 34**: Magic strings in error messages
```csharp
// ❌ BEFORE
throw new ArgumentException("Email list cannot be null or empty.");
throw new ArgumentException("Email templates cannot be null or empty.");

// ✅ AFTER
throw new ArgumentException(ErrorMessages.EmailListEmpty);
throw new ArgumentException(ErrorMessages.EmailTemplatesEmpty);
```

**Line 43**: Dynamic error message
```csharp
// ❌ BEFORE
throw new InvalidOperationException($"No email template found for {invitation.MessageToSend}");

// ✅ AFTER
throw new InvalidOperationException(string.Format(ErrorMessages.NoEmailTemplate, invitation.MessageToSend));
```

---

### 2. ParticipantService.cs

**Lines 316, 359**: Magic number `1` for status
```csharp
// ❌ BEFORE
EvaluationParticipantStatusId = 1,

// ✅ AFTER
EvaluationParticipantStatusId = EvaluationParticipantStatusIds.InvitationNotSent,
```

---

### 3. AdminController.cs

**Line 83**: Hardcoded "Admin" role (inconsistent - should use RoleNames.Admin)
```csharp
// ❌ BEFORE
[Authorize(Roles = "Admin")]

// ✅ AFTER
[Authorize(Roles = RoleNames.Admin)]
```

**Line 95**: Error message
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "No participants selected for reminders.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.NoParticipantsSelected);
```

**Line 109**: Error message
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "Failed to process reminders.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.FailedToProcessReminders);
```

**Line 160**: ViewData key and partial view name
```csharp
// ❌ BEFORE
ViewData["ActiveClient"] = LocalClientId;
return PartialView("_ManageClientsPartial", clients);

// ✅ AFTER
ViewData[ViewDataKeys.ActiveClient] = LocalClientId;
return PartialView(PartialViewNames.ManageClientsPartial, clients);
```

**Line 199**: Error message
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "An error occurred while saving the participant.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.FailedToSaveParticipant);
```

**Line 211**: Error message
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "Invalid participantId.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.InvalidParticipantId);
```

**Line 220**: Action name
```csharp
// ❌ BEFORE
return RedirectToAction("Error");

// ✅ AFTER
return RedirectToAction(ActionNames.Error);
```

**Lines 271, 329, 365**: Error messages for invalid data
```csharp
// ❌ BEFORE
return BadRequest("Invalid Evaluation data.");
return BadRequest("Invalid Client data.");
return BadRequest("Invalid Email Template data.");

// ✅ AFTER
return BadRequest(ErrorMessages.InvalidEvaluationData);
return BadRequest(ErrorMessages.InvalidClientData);
return BadRequest(ErrorMessages.InvalidEmailTemplateData);
```

**Lines 336, 372, 401**: Save failure messages
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "Failed to save client.");
ModelState.AddModelError("", "Failed to save email template.");
ModelState.AddModelError("", "Failed to process invitations.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.FailedToSaveClient);
ModelState.AddModelError("", ErrorMessages.FailedToSaveEmailTemplate);
ModelState.AddModelError("", ErrorMessages.FailedToProcessInvitations);
```

**Lines 435, 438, 448, 451**: Email queue actions
```csharp
// ❌ BEFORE
return BadRequest("Email not found or already sent.");
return RedirectToAction("EmailQueueDashboard");

// ✅ AFTER
return BadRequest(ErrorMessages.EmailNotFoundOrSent);
return RedirectToAction(ActionNames.EmailQueueDashboard);
```

---

### 4. ClientController.cs

**Lines 89, 137**: Error messages
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "Failed to create client.");
ModelState.AddModelError("", "Failed to update client.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.FailedToCreateClient);
ModelState.AddModelError("", ErrorMessages.FailedToUpdateClient);
```

---

### 5. ParticipantController.cs

**Line 97**: Error message
```csharp
// ❌ BEFORE
return BadRequest("Invalid request. Please provide a valid Participant ID.");

// ✅ AFTER
return BadRequest(ErrorMessages.InvalidRequest);
```

**Lines 177, 194**: Error messages
```csharp
// ❌ BEFORE
ModelState.AddModelError("", "Failed to add new rater.");
ModelState.AddModelError("", "Failed to add rater.");

// ✅ AFTER
ModelState.AddModelError("", ErrorMessages.FailedToAddNewRater);
ModelState.AddModelError("", ErrorMessages.FailedToAddRater);
```

**Lines 215, 236, 263**: Error messages
```csharp
// ❌ BEFORE
return BadRequest("Invalid evaluation ratee or participant ID.");
return BadRequest("Unable to launch evaluation.");
return BadRequest("Unable to process completed survey.");

// ✅ AFTER
return BadRequest(ErrorMessages.InvalidEvaluationRateeId);
return BadRequest(ErrorMessages.UnableToLaunchEvaluation);
return BadRequest(ErrorMessages.UnableToProcessSurvey);
```

**Line 236**: Action name (if used for redirect)
```csharp
// ❌ BEFORE (hypothetical)
return RedirectToAction("ViewRaters");

// ✅ AFTER
return RedirectToAction(ActionNames.ViewRaters);
```

---

### 6. MigrationController.cs

Multiple error/success messages:
```csharp
// Lines 48, 205, 211, 222, 227, 234, 243, 260, 275
// Replace with:
ErrorMessages.MigrationDisabled
ErrorMessages.MigrationDataExists
ErrorMessages.MigrationLegacyNotFound
ErrorMessages.MigrationRolledBack
SuccessMessages.MigrationCompleted
ErrorMessages.MigrationFailed
ErrorMessages.ValidationFailed
```

---

### 7. InvitationService.cs

**Line 58**: Already uses StatusNames.ReminderGeneral ✅

Consider adding for consistency:
```csharp
// Could extract the template name query to use EmailTemplateNames
.Where(p => p.ClientId == clientId && p.EmailTemplateName == EmailTemplateNames.ReminderGeneral)
```

---

## 📊 Summary Statistics

- **Total Constants Added**: 60+
- **Files Requiring Updates**: 7 main files
- **High Priority**: EmailQueueHelper.cs (hardcoded status IDs)
- **Medium Priority**: All controllers (error messages)
- **Low Priority**: Action names, view names, ViewData keys

---

## ✨ Benefits of Refactoring

1. **Type Safety**: Compile-time checking instead of runtime string errors
2. **IntelliSense**: Auto-completion for all constant values
3. **Refactoring Safety**: Rename constants in one place
4. **Consistency**: Same error messages across the application
5. **Maintainability**: Single source of truth for all magic strings
6. **Documentation**: Constants provide context for what strings mean

---

## 🚀 Recommended Implementation Order

1. **Phase 1** (Immediate): Replace status IDs in EmailQueueHelper and ParticipantService
2. **Phase 2** (Short-term): Replace all error messages in controllers
3. **Phase 3** (Medium-term): Replace action names, view names, ViewData keys
4. **Phase 4** (Long-term): Add remaining strings from views and migrations

---

## ⚠️ Important Notes

- Always add `using reviews.Constants;` to files using these constants
- Some strings in Migration files are for one-time data seeding (low priority to change)
- String interpolation messages should use `string.Format()` with constant templates
- Consider creating a unit test to verify all database status values match constants
