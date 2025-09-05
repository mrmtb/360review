using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class MigrateData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            var sqlToRun = "";

            sqlToRun = @"
insert into public.""Client"" (
        ""ClientId"", 
        ""ClientName"", 
        ""ClientLogoLocation"", 
        ""ClientHelpContact"",
        ""ClientIsActive"", 
        ""ClientEmailDomain""
    )
    values
    (
        gen_random_uuid(), 
        'MomentiveTech', 
        '/images/Momentive-Technologies-Logo2.png',
        'Christopher Rotolo, chris@behavioralinsights.com or 914.299.6298',
        TRUE, 
        '@momentivetech.com'
    );";

            migrationBuilder.Sql(sqlToRun);

            sqlToRun =
@"INSERT INTO public.""EmailTemplate"" (
    ""ClientId"",
    ""EmailTemplateName"",
    ""EmailFromName"",
    ""EmailSubject"",
    ""EmailTemplateContent""
)
SELECT
    c.""ClientId"",
    'Invitation - Rater',
    '*ClientName* 360 Administration <MT360@behavioralinsights.com>',
    'Invitation to provide 360-degree feedback for *RateeFirstName* *RateeLastName*',
    'Dear *RaterFirstName*,

One of your colleagues, *RateeFirstName* *RateeLastName*, is participating in *ClientName*''s 360-degree feedback program and has identified you as a *RaterRole*.  The program provides *RateeFirstName* with feedback from his/her direct reports and peers on *ClientName*''s Leadership Competencies and behaviors that are critical to success at *ClientName*.  The feedback is for development purposes only and individual rater feedback is completely anonymous to the participant.  Comments will be combined with other responses and presented in random order to protect anonymity. *RateeFirstName* has requested your candid feedback.

Please take 10-15 minutes and click on the link below to provide your feedback for *RateeFirstName*.

*Url*

To complete your assessment, log in to your Leadership Feedback Portal by using the link above. When you access the site for the first time, you''ll need to register for an account with your work email address. Please do not use your work password when creating the account. If you ever forget your password, you'll be able to recover it through your email.

Please complete your feedback by *DueDate*.

Should you have any questions about the 360-degree process, or your participation in it, please contact:

*ClientHelpContact*'
FROM
    public.""Client"" c;";

            migrationBuilder.Sql(sqlToRun);

            sqlToRun =
@"INSERT INTO public.""EmailTemplate"" (
    ""ClientId"",
    ""EmailTemplateName"",
    ""EmailFromName"",
    ""EmailSubject"",
    ""EmailTemplateContent""
)
SELECT
    c.""ClientId"",
    'Invitation - Ratee',
    '*ClientName* 360 Administration <MT360@behavioralinsights.com>',
    'Invitation to participate in *ClientName*'s 360-degree feedback process',
    'Dear *RateeFirstName*,

Congratulations on agreeing to participate in *ClientName*''s 360-degree feedback process. The process of gaining feedback from others is a powerful tool for increasing self-awareness and motivating professional development. We are confident that you will gain valuable insights from this experience. 

As you know, this process involves gaining feedback from your peers and direct reports, as well as your own self-assessment.  Your peers and direct reports have automatically been invited to rate you.  To complete your self-assessment, you need to log in to your Leadership Feedback Portal by using the following link:

*Url*

To complete your self-assessment, you need to log in to your Leadership Feedback Portal by using the above link. When you access the site for the first time, you''ll need to register for an account with your work email address. Please do not use your work password when creating the account. If you ever forget your password, you''ll be able to recover it through your email.

We ask that you complete your self-assessment within the next two weeks. 

Should you have any questions about the 360-degree process, or your participation in it, please feel free to contact us at:

*ClientHelpContact*'
FROM
    public.""Client"" c;";

            migrationBuilder.Sql(sqlToRun);

            sqlToRun =
@"INSERT INTO public.""EmailTemplate"" (
    ""ClientId"",
    ""EmailTemplateName"",
    ""EmailFromName"",
    ""EmailSubject"",
    ""EmailTemplateContent""
)
SELECT
    c.""ClientId"",
    'Reminder - General',
    '*ClientName* 360 Administration <MT360@behavioralinsights.com>',
    'REMINDER: Participate in *ClientName*'s 360-degree feedback process',
    'Dear *RaterFirstName*,

Please visit the *ClientName* 360-degree feedback site to complete your pending evaluation(s):

*Url*

To complete your assessment, you need to log in to your Leadership Feedback Portal by using the above link. When you access the site for the first time, you''ll register for an account with your work email address. Please do not use your work password when creating the account. If you ever forget your password, you''ll be able to recover it through your email.

Please complete your feedback by *DueDate*.

Should you have any questions about the 360-degree process, or your participation in it, please contact:

*ClientHelpContact*

NOTE: This is a reminder email.  If you did not receive an original invitation, please check your junk folder as Momentive Tech IT policies may have directed them there.'
FROM
    public.""Client"" c;";

            migrationBuilder.Sql(sqlToRun);

            sqlToRun = @"
INSERT INTO public.""EmailTemplate""
    ( ""ClientId"",
    ""EmailTemplateName"",
    ""EmailFromName"",
    ""EmailSubject"",
    ""EmailTemplateContent"")
SELECT
    c.""ClientId"",
    m.msg_purpose,
    m.msg_from,
    m.msg_subject,
    m.msg_body
FROM
    public.""Client"" c,
    legacy.""360_messages"" m
;";

            migrationBuilder.Sql(sqlToRun);

            migrationBuilder.Sql(@"
INSERT INTO public.""Participant"" (
    ""ParticipantNameFirst"", 
    ""ParticipantNameLast"", 
    ""ParticipantEmail"", 
    ""ParticipantHasRegistered"",
    ""ParticipantIsActive"",
    ""ClientId""
) 
SELECT 
        name_first, 
        name_last, 
        email, 
        False, 
        True,
        ""ClientId"" 
FROM    legacy.""360_colleagues"", 
        public.""Client"";");

            // manual creation of the survey windows (evaluation in new system)
            migrationBuilder.Sql(@"
insert into 
public.""Evaluation"" (
    --""EvaluationId"",
    ""EvaluationActive"", 
    ""EvaluationName"", 
    ""EvaluationFolder"", 
    ""EvaluationHelpContact"",
    ""EvaluationMaxRaters"",
    ""EvaluationOpenDate"", 
    ""EvaluationRateeDueDate"", 
    ""EvaluationRaterDueDate"", 
    ""EvaluationClientContact"", 
    ""ClientId""
)
SELECT
    --1,
    FALSE, 
    '2022 Momentive Survey', 
    '/momentivesurvey3/momentive360.asp', 
    NULL,
    15,
    '2022-05-01', 
    '2022-06-15', 
    '2022-06-30', 
    NULL, 
    cl.""ClientId""
FROM
    public.""Client"" cl;");

            // manual creation of the survey windows (evaluation in new system)
            migrationBuilder.Sql(@"
insert into 
public.""Evaluation"" (
    --""EvaluationId"", 
    ""EvaluationActive"", 
    ""EvaluationName"", 
    ""EvaluationFolder"", 
    ""EvaluationHelpContact"",
    ""EvaluationMaxRaters"",
    ""EvaluationOpenDate"", 
    ""EvaluationRateeDueDate"", 
    ""EvaluationRaterDueDate"", 
    ""EvaluationClientContact"", 
    ""ClientId""
)
SELECT
    --2,
    FALSE, 
    '2023 Momentive Survey', 
    '/momentivesurvey3/momentive360.asp', 
    NULL,
    15,
    '2023-05-15', 
    '2023-06-15', 
    '2023-06-30', 
    NULL, 
    cl.""ClientId""
FROM
    public.""Client"" cl;");

            // manual creation of the survey windows (evaluation in new system)
            migrationBuilder.Sql(@"
insert into 
public.""Evaluation"" (
    --""EvaluationId"",
    ""EvaluationActive"", 
    ""EvaluationName"", 
    ""EvaluationFolder"", 
    ""EvaluationHelpContact"",
    ""EvaluationMaxRaters"",
    ""EvaluationOpenDate"", 
    ""EvaluationRateeDueDate"", 
    ""EvaluationRaterDueDate"", 
    ""EvaluationClientContact"", 
    ""ClientId""
)
SELECT
    --3,
    TRUE, 
    '2024 Momentive Survey', 
    '/momentivesurvey3/momentive360.asp', 
    NULL,
    15,
    '2024-03-26', 
    '2024-05-15', 
    '2024-05-30', 
    NULL, 
    cl.""ClientId""
FROM
    public.""Client"" cl;");

            // manual creation of the survey windows (evaluation in new system)
            migrationBuilder.Sql(@"
insert into 
public.""Evaluation"" (
    --""EvaluationId"",
    ""EvaluationActive"", 
    ""EvaluationName"", 
    ""EvaluationFolder"", 
    ""EvaluationHelpContact"",
    ""EvaluationMaxRaters"",
    ""EvaluationOpenDate"", 
    ""EvaluationRateeDueDate"", 
    ""EvaluationRaterDueDate"", 
    ""EvaluationClientContact"", 
    ""ClientId""
)
SELECT
    --4,
    TRUE, 
    '2025 Momentive Survey', 
    '/momentivesurvey3/momentive360.asp', 
    NULL,
    15,
    '2025-05-01', 
    '2025-09-15', 
    '2025-09-30', 
    NULL, 
    cl.""ClientId""
FROM
    public.""Client"" cl;");

            migrationBuilder.Sql(@"
INSERT INTO ""EvaluationRateeStatus"" ( ""EvaluationRateeStatusId"", ""EvaluationRateeStatusName"", ""OnReminderNewStatus"", ""OnCompletionNewStatus"", ""OnOptOutNewStatus"")
VALUES (1, 'Invitation Not Sent',   1, 7, 8), 
    (2, 'Pending Approval',         1, 7, 8), 
    ( 3, 'Manager Declined',        1, 7, 8), 
    ( 4, 'Invitation Sent',         1, 7, 8), 
    ( 5, 'Opt-out',                 5, 5, 5), 
    ( 6, 'Unfamiliar',              1, 7, 8), 
    ( 7, 'Completed',               1, 7, 8), 
    ( 8, 'Deleted',                 1, 7, 8);");


            migrationBuilder.Sql(@"
INSERT INTO ""EvaluationParticipantStatus"" ( ""EvaluationParticipantStatusId"", ""EvaluationParticipantStatusName"", ""OnReminderNewStatus"", ""OnCompletionNewStatus"", ""OnOptOutNewStatus"", ""EvaluationParticipantStatusIsActive"", ""EvaluationParticipantStatusIsComplete"", ""EvaluationParticipantStatusDecoration"")
VALUES 
    ( 1, 'Invitation Not Sent', 1, 7, 8, 1, 0, '<i class=""bi bi-arrow-right-square""></i>'), 
    ( 2, 'Pending Approval',    1, 7, 8, 1, 0, '<i class=""bi bi-arrow-right-square""></i>'), 
    ( 3, 'Manager Declined',    1, 7, 8, 0, 0, '<i class=""bi bi-exclamation-diamond"" style=""color: rgb(216, 51, 51);""></i>'), 
    ( 4, 'Invitation Sent',     1, 7, 8, 1, 0, '<i class=""bi bi-arrow-right-square""></i>'), 
    ( 5, 'Opt-out',             5, 5, 5, 0, 0, '<i class=""bi bi-arrow-right-square""></i>'), 
    ( 6, 'Unfamiliar',          1, 7, 8, 0, 0, '<i class=""bi bi-question-circle""></i>'), 
    ( 7, 'Completed',           1, 7, 8, 1, 1, '<i class=""bi bi-check-square"" style=""color: rgb(14, 174, 27);""></i>'), 
    ( 8, 'Deleted',             1, 7, 8, 0, 0, '<i class=""bi bi-exclamation-diamond"" style=""color: rgb(216, 51, 51);""></i>');");

            migrationBuilder.Sql(@"
INSERT INTO ""ParticipantRole"" (
    ""ParticipantRoleId"",
    ""ParticipantRoleName""     )
values
    (1, 'Self'), 
    (2, 'Manager'),
    (3, 'Peer'),
    (4, 'Direct Report');");

            migrationBuilder.Sql(@"
insert into 
    public.""EvaluationRatee"" (
    ""EvaluationId"", 
    ""ParticipantId"", 
    ""EvaluationRateeStatusId"", 
    ""EvaluationRateeDueDateOverride"", 
    ""EvaluationRaterDueDateOverride"",
    ""EvaluationRateeDateAdded""
    )
SELECT e.""EvaluationId"", ""ParticipantId"", ers.""EvaluationRateeStatusId"", NULL, NULL, ev.added
FROM legacy.""360_colleagues"" co 
INNER JOIN legacy.""360_evaluations"" ev ON co.participant_id = ev.participant_id 
INNER JOIN public.""Participant"" pa ON co.email = pa.""ParticipantEmail"" 
INNER JOIN public.""EvaluationRateeStatus"" ers ON ev.eval_status = ers.""EvaluationRateeStatusName"" 
INNER JOIN public.""Evaluation"" e ON ev.added >= e.""EvaluationOpenDate"" AND ev.added <= e.""EvaluationRaterDueDate""
WHERE ev.relationship = 'Self';");

            migrationBuilder.Sql(@"
insert into 
public.""EvaluationParticipant"" (
    ""EvaluationRateeId"", 
    ""ParticipantId"", 
    ""ParticipantRoleId"", 
    ""EvaluationParticipantSurveyPassword"", 
    ""EvaluationParticipantDateLastEmailed"", 
    ""EvaluationParticipantStatusId"",
    ""EvaluationParticipantDateAdded"",
    ""EvaluationParticipantSessionId"",
    ""EvaluationParticipantDateCompleted""
)
SELECT 
    er.""EvaluationRateeId"", 
    pa.""ParticipantId"", 
    pr.""ParticipantRoleId"", 
    ev.svy_password, 
    co.last_msg, 
    eps.""EvaluationParticipantStatusId"", 
    added,
    svy_session,
    to_timestamp(svy_fdt, 'Mon DD YYYY HH24:MI')
FROM legacy.""360_colleagues"" co 
INNER JOIN legacy.""360_evaluations"" ev ON co.participant_id = ev.participant_id 
INNER JOIN legacy.""360_colleagues"" co2 ON ev.ratee_id = co2.participant_id
INNER JOIN public.""ParticipantRole"" pr ON ev.relationship = pr.""ParticipantRoleName""
INNER JOIN public.""Participant"" pa ON co.email = pa.""ParticipantEmail"" 
INNER JOIN public.""Participant"" pa2 ON co2.email = pa2.""ParticipantEmail""
INNER JOIN public.""EvaluationRatee"" er ON pa2.""ParticipantId"" = er.""ParticipantId""
INNER JOIN public.""EvaluationParticipantStatus"" eps ON ev.eval_status = eps.""EvaluationParticipantStatusName""  
WHERE ev.eval_id not in (164, 307, 491);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
