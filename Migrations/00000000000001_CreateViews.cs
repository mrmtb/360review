using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class _00000000000001_CreateViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwAdminParticipants"" AS
                WITH er AS (
                SELECT
                    er_1.""ParticipantId"",
                    e_1.""EvaluationId"",
                    e_1.""EvaluationActive""
                FROM
                    ""EvaluationRatee"" er_1
                JOIN ""Evaluation"" e_1
                    ON er_1.""EvaluationId"" = e_1.""EvaluationId""
                    AND e_1.""EvaluationActive"" = true
                ),
                ep AS (
                SELECT
                    ep_1.""ParticipantId"",
                    e_2.""EvaluationId"",
                    e_2.""EvaluationActive"",
                    ep_1.""EvaluationParticipantDateLastEmailed""
                FROM
                    ""EvaluationRatee"" er_2
                JOIN ""EvaluationParticipant"" ep_1
                    ON er_2.""EvaluationRateeId"" = ep_1.""EvaluationRateeId""
                JOIN ""Evaluation"" e_2
                    ON er_2.""EvaluationId"" = e_2.""EvaluationId""
                ), 
                p AS (
                SELECT 
                    DISTINCT ON (p.""ParticipantId"")
                    c.""ClientId"",
                    c.""ClientName"",
                    p.""ParticipantId"", 
                    p.""ParticipantNameFirst"", 
                    p.""ParticipantNameLast"", 
                    p.""ParticipantEmail"",
                    p.""ParticipantIsActive"", 
                    CASE 
                        WHEN er.""ParticipantId"" IS NOT NULL THEN 'Yes' 
                        ELSE '' END ""IsRatee"", 
                    CASE 
                        WHEN p.""ParticipantIsActive"" THEN '' 
                        ELSE 'No' END ""IsActive"", 
                    ep.""EvaluationParticipantDateLastEmailed"" 
                FROM 
                                public.""Client"" c
                    INNER JOIN  public.""Participant"" p 
                    ON          c.""ClientId"" = p.""ClientId""
                    LEFT JOIN er
                    ON p.""ParticipantId"" = er.""ParticipantId""
                    LEFT JOIN ep
                    ON p.""ParticipantId"" = ep.""ParticipantId""
                    ORDER BY 
                    p.""ParticipantId"",
                    ep.""EvaluationParticipantDateLastEmailed"" DESC)
                    SELECT
                    p.""ClientId"",
                    p.""ClientName"",
                    p.""ParticipantId"",
                    p.""ParticipantNameFirst"",
                    p.""ParticipantNameLast"",
                    p.""ParticipantEmail"",
                    p.""ParticipantIsActive"",
                    p.""IsRatee"",
                    p.""IsActive"",
                    p.""EvaluationParticipantDateLastEmailed""
                    FROM p
                    ORDER BY p.""ParticipantNameFirst"", p.""ParticipantNameLast""
                    ;");

            migrationBuilder.Sql(@"
              CREATE OR REPLACE VIEW public.""vwAdminRatees"" AS
              WITH ar AS (
                SELECT
                    DISTINCT ON (p.""ParticipantId"")
                    c.""ClientId"",
                    c.""ClientName"",
                    e.""EvaluationId"",
                    p.""ParticipantId"", 
                    p.""ParticipantNameFirst"", 
                    p.""ParticipantNameLast"", 
                    p.""ParticipantEmail"", 
                    p.""ParticipantIsActive"",
                    er.""EvaluationRateeDateAdded"",
                    ep.""EvaluationParticipantDateLastEmailed"",
                    COALESCE(   ""EvaluationRateeDueDateOverride"", 
                                ""EvaluationRateeDueDateOverride"", 
                                ""EvaluationRaterDueDate"", 
                                ""EvaluationRateeDueDate"") as ""LastDueDate""
                FROM 
                                public.""Client"" c
                    INNER JOIN  public.""Evaluation"" e
                    ON      c.""ClientId"" = e.""ClientId""
                    INNER JOIN  public.""EvaluationRatee"" er 
                    ON      e.""EvaluationId"" = er.""EvaluationId""
                    INNER JOIN  public.""EvaluationParticipant"" ep
                    ON      er.""EvaluationRateeId"" = ep.""EvaluationRateeId""
                    AND     er.""ParticipantId"" = ep.""ParticipantId""
                    INNER JOIN  public.""Participant"" p 
                    ON     er.""ParticipantId"" = p.""ParticipantId""
                WHERE
                    e.""EvaluationActive"" = TRUE AND
                    p.""ParticipantIsActive"" = TRUE
                ORDER BY 
                    p.""ParticipantId"",
                    e.""EvaluationRateeDueDate"" DESC )
                SELECT
                    ar.""ClientId"",
                    ar.""ClientName"",
                    ar.""EvaluationId"",
                    ar.""ParticipantId"",
                    ar.""ParticipantNameFirst"",
                    ar.""ParticipantNameLast"",
                    ar.""ParticipantEmail"",
                    ar.""ParticipantIsActive"",
                    ar.""EvaluationRateeDateAdded"",
                    ar.""EvaluationParticipantDateLastEmailed"",
                    ar.""LastDueDate""
                FROM ar
                ORDER BY
                    ar.""ParticipantNameLast"",
                    ar.""ParticipantNameFirst"";");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwRatersByRatee"" AS
                SELECT 
                    er.""EvaluationRateeId"",
                    p.""ParticipantId"" AS ""Ratee"",
                    e.""EvaluationId"",
                    e.""EvaluationActive"",
                    e.""EvaluationTestEmails"",
                    e.""EvaluationMaxRaters"",
                    p2.""ParticipantId"", 
                    p2.""ParticipantNameFirst"", 
                    p2.""ParticipantNameLast"", 
                    p2.""ParticipantEmail"", 
                    pr.""ParticipantRoleId"",
                    pr.""ParticipantRoleName"",
                    ep.""EvaluationParticipantDateLastEmailed"",
                    eps.""EvaluationParticipantStatusId"",
                    eps.""EvaluationParticipantStatusName"",
                    eps.""EvaluationParticipantStatusIsActive"",
                    eps.""EvaluationParticipantStatusIsComplete"",
                    eps.""EvaluationParticipantStatusDecoration""
                FROM 
                                public.""Participant"" p 
                    INNER JOIN  public.""EvaluationRatee"" er 
                    ON      p.""ParticipantId"" = er.""ParticipantId"" 
                    INNER JOIN  public.""Evaluation"" e
                    ON      er.""EvaluationId"" = e.""EvaluationId""
                    INNER JOIN  public.""EvaluationParticipant"" ep 
                    ON      er.""EvaluationRateeId"" = ep.""EvaluationRateeId"" 
                    INNER JOIN  public.""Participant"" p2
                    ON      ep.""ParticipantId"" = p2.""ParticipantId""
                    INNER JOIN  public.""ParticipantRole"" pr
                    ON      ep.""ParticipantRoleId"" = pr.""ParticipantRoleId""
                    INNER JOIN  public.""EvaluationParticipantStatus"" eps
                    ON      ep.""EvaluationParticipantStatusId"" = eps.""EvaluationParticipantStatusId""
                WHERE
                    p2.""ParticipantIsActive"" = TRUE
                ORDER BY    
                    p.""ParticipantEmail"" ASC,
                    pr.""ParticipantRoleId"" ASC,
                    p2.""ParticipantNameLast"" ASC,
                    p2.""ParticipantNameFirst"" ASC;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwEvaluationsToComplete"" AS
                SELECT 
                    p.""ParticipantId"" AS ""Rater"",
                    p.""ParticipantNameFirst"" AS ""RaterNameFirst"",
                    p.""ParticipantNameLast"" AS ""RaterNameLast"",
                    p.""ParticipantEmail"" AS ""RaterEmail"",
                    e.""EvaluationId"",
                    e.""EvaluationActive"",
                    e.""EvaluationTestEmails"",
                    e.""EvaluationName"",
                    e.""EvaluationFolder"",
                    ep.""EvaluationParticipantSurveyPassword"",
                    p2.""ParticipantId"", 
                    p2.""ParticipantNameFirst"", 
                    p2.""ParticipantNameLast"", 
                    p2.""ParticipantEmail"",
                    er.""EvaluationRateeId"",
                    pr.""ParticipantRoleId"",
                    pr.""ParticipantRoleName"",
                    ep.""EvaluationParticipantDateLastEmailed"",
                    COALESCE(er.""EvaluationRateeDueDateOverride"", er.""EvaluationRateeDueDateOverride"", e.""EvaluationRaterDueDate"") AS ""DueDate"",
                    eps.""EvaluationParticipantStatusName"",
                    eps.""EvaluationParticipantStatusIsActive"",
                    eps.""EvaluationParticipantStatusIsComplete"", 
                    eps.""EvaluationParticipantStatusDecoration""
                FROM 
                                public.""Evaluation"" e
                    INNER JOIN  public.""EvaluationRatee"" er 
                    ON      e.""EvaluationId"" = er.""EvaluationId""
                    INNER JOIN  public.""EvaluationParticipant"" ep 
                    ON      er.""EvaluationRateeId"" = ep.""EvaluationRateeId""
                    INNER JOIN  public.""Participant"" p 
                    ON      ep.""ParticipantId"" = p.""ParticipantId""
                    INNER JOIN  public.""Participant"" p2
                    ON      er.""ParticipantId"" = p2.""ParticipantId""
                    INNER JOIN  public.""ParticipantRole"" pr
                    ON      ep.""ParticipantRoleId"" = pr.""ParticipantRoleId""
                    INNER JOIN  public.""EvaluationParticipantStatus"" eps
                    ON      ep.""EvaluationParticipantStatusId"" = eps.""EvaluationParticipantStatusId""
                WHERE
                    p.""ParticipantIsActive"" = TRUE AND
                    p2.""ParticipantIsActive"" = TRUE AND
                    e.""EvaluationActive"" = TRUE
                ORDER BY    
                    p.""ParticipantEmail"" ASC,
                    pr.""ParticipantRoleId"" ASC,
                    p2.""ParticipantNameLast"" ASC,
                    p2.""ParticipantNameFirst"" ASC;");

            migrationBuilder.Sql(@"
              CREATE OR REPLACE VIEW ""vwAdminRatees"" AS
              WITH ar AS (
                SELECT
                    DISTINCT ON (p.""ParticipantId"")
                    c.""ClientId"",
                    c.""ClientName"",
                    e.""EvaluationId"",
                    p.""ParticipantId"", 
                    p.""ParticipantNameFirst"", 
                    p.""ParticipantNameLast"", 
                    p.""ParticipantEmail"", 
                    p.""ParticipantIsActive"",
                    er.""EvaluationRateeDateAdded"",
                    ep.""EvaluationParticipantDateLastEmailed"",
                    COALESCE(   ""EvaluationRateeDueDateOverride"", 
                                ""EvaluationRateeDueDateOverride"", 
                                ""EvaluationRaterDueDate"", 
                                ""EvaluationRateeDueDate"") as ""LastDueDate""
                FROM 
                                public.""Client"" c
                    INNER JOIN  public.""Evaluation"" e
                    ON      c.""ClientId"" = e.""ClientId""
                    INNER JOIN  public.""EvaluationRatee"" er 
                    ON      e.""EvaluationId"" = er.""EvaluationId""
                    INNER JOIN  public.""EvaluationParticipant"" ep
                    ON      er.""EvaluationRateeId"" = ep.""EvaluationRateeId""
                    AND     er.""ParticipantId"" = ep.""ParticipantId""
                    INNER JOIN  public.""Participant"" p
                    ON     er.""ParticipantId"" = p.""ParticipantId""
                WHERE
                    e.""EvaluationActive"" = TRUE AND
                    p.""ParticipantIsActive"" = TRUE
                ORDER BY p.""ParticipantId"" )
                SELECT
                    ar.""ClientId"",
                    ar.""ClientName"",
                    ar.""EvaluationId"",
                    ar.""ParticipantId"",
                    ar.""ParticipantNameFirst"",
                    ar.""ParticipantNameLast"",
                    ar.""ParticipantEmail"",
                    ar.""ParticipantIsActive"",
                    ar.""EvaluationRateeDateAdded"",
                    ar.""EvaluationParticipantDateLastEmailed"",
                    ar.""LastDueDate""
                FROM ar
                ORDER BY
                    ar.""ParticipantNameLast"",
                    ar.""ParticipantNameFirst"";");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""vwEvaluationsByRatee"" AS
                SELECT  DISTINCT
                        e.""EvaluationId"",
                        e.""EvaluationActive"",
                        e.""EvaluationName"",
                        e.""EvaluationFolder"",
                        e.""EvaluationHelpContact"",
                        e.""EvaluationMaxRaters"",
                        e.""EvaluationOpenDate"",
                        e.""EvaluationRateeDueDate"",
                        e.""EvaluationRaterDueDate"",
                        e.""EvaluationClientContact"",
                        e.""ClientId"",
                        ep.""ParticipantId""
                FROM public.""Evaluation"" e
                INNER JOIN  public.""EvaluationRatee"" er
                ON      e.""EvaluationId"" = er.""EvaluationId""
                INNER JOIN  public.""EvaluationParticipant"" ep
                ON      er.""EvaluationRateeId"" = ep.""EvaluationRateeId"";");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwReminders"" AS
                SELECT 
                    c.""ClientId"",
                    c.""ClientName"",
                    c.""ClientHelpContact"",
                    e.""EvaluationId"",
                    er.""EvaluationRateeId"",
                    r.""ParticipantId"" AS ""RateeParticipantId"",
                    r.""ParticipantNameFirst"" as ""RateeNameFirst"",
                    r.""ParticipantNameLast"" as ""RateeNameLast"",
                    CONCAT(r.""ParticipantNameLast"", ', ', r.""ParticipantNameFirst"") as ""RateeNameCombined"",
                    r.""ParticipantEmail"" as ""RateeEmail"",
                    p.""ParticipantId"" AS ""ParticipantId"",
                    p.""ParticipantNameFirst"" as ""ParticipantNameFirst"",
                    p.""ParticipantNameLast"" as ""ParticipantNameLast"",
                    CONCAT(p.""ParticipantNameLast"", ', ', p.""ParticipantNameFirst"") as ""ParticipantNameCombined"",
                    p.""ParticipantEmail"" as ""ParticipantEmail"",
                    pr.""ParticipantRoleName"",
                    'Reminder - General' AS ""MessageToSend"",
                    ep.""EvaluationParticipantDateAdded"",
                    ep.""EvaluationParticipantDateLastEmailed"",
                    COALESCE(   er.""EvaluationRateeDueDateOverride"", 
                                er.""EvaluationRateeDueDateOverride"", 
                                e.""EvaluationRaterDueDate"", 
                                e.""EvaluationRateeDueDate"") as ""DueDate""
                FROM 
                                public.""Client"" c
                    INNER JOIN  public.""Evaluation"" e
                    ON      c.""ClientId"" = e.""ClientId""
                    INNER JOIN  public.""EvaluationRatee"" er 
                    ON      e.""EvaluationId"" = er.""EvaluationId""
                    INNER JOIN  public.""EvaluationParticipant"" ep
                    ON      er.""EvaluationRateeId"" = ep.""EvaluationRateeId""
                    INNER JOIN  public.""ParticipantRole"" pr
                    ON      ep.""ParticipantRoleId"" = pr.""ParticipantRoleId""
                    INNER JOIN  public.""Participant"" r
                    ON     er.""ParticipantId"" = r.""ParticipantId""
                    INNER JOIN public.""Participant"" p
                    ON      ep.""ParticipantId"" = p.""ParticipantId""
                    INNER JOIN public.""EvaluationParticipantStatus"" eps
                    ON      ep.""EvaluationParticipantStatusId"" = eps.""EvaluationParticipantStatusId""
                WHERE eps.""EvaluationParticipantStatusName"" = 'Invitation Sent'
                    AND     e.""EvaluationActive"" = TRUE
                    AND     p.""ParticipantIsActive"" = TRUE
                    AND     r.""ParticipantIsActive"" = TRUE
                ORDER BY    
                    r.""ParticipantNameLast"",
                    r.""ParticipantNameFirst"",
                    p.""ParticipantNameLast"",
                    p.""ParticipantNameFirst"";");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""vwParticipant"" AS
                SELECT 
                            p.""ParticipantId"",
                            p.""ParticipantNameFirst"",
                            p.""ParticipantNameLast"",
                            p.""ParticipantEmail"",
                            COALESCE(anu.""Email"", p.""ParticipantEmail"") AS ""Email"",
                            p.""ParticipantHasRegistered"",
                            COALESCE(anu.""EmailConfirmed"", FALSE) AS ""EmailConfirmed"",
                            p.""ParticipantIsActive"",
                            COALESCE(anu.""LockoutEnabled"", FALSE) AS ""LockoutEnabled"",
                            COALESCE(anu.""LockoutEnd"", CURRENT_TIMESTAMP) AS ""LockoutEnd"",
                            COALESCE(anu.""AccessFailedCount"", -1) AS ""AccessFailedCount""
                FROM 
                            public.""Participant"" p
                LEFT JOIN   public.""AspNetUsers"" anu
                ON          p.""ParticipantEmail"" = anu.""Email"";");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwDefaultEvaluationByParticipant"" AS
                WITH er AS (
                    SELECT      er.""ParticipantId"",
                                er.""EvaluationId"",
                                e.""EvaluationActive""
                    FROM        public.""EvaluationRatee"" er 
                    INNER JOIN  public.""Evaluation"" e
                    ON      er.""EvaluationId"" = e.""EvaluationId""
                )
                SELECT 
                    DISTINCT ON (p.""ParticipantId"")
                    p.""ParticipantId"",
                    COALESCE(er.""EvaluationId"", 0) ""EvaluationId"",
                    COALESCE(er.""EvaluationActive"", false) ""EvaluationActive""
                FROM 
                                public.""Participant"" p 
                    LEFT JOIN   er
                    ON      p.""ParticipantId"" = er.""ParticipantId""
                ORDER BY    
                    p.""ParticipantId"" ASC,
                    er.""EvaluationActive"" DESC,
                    er.""EvaluationId"" DESC;");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwRaterInvitations"" AS
                SELECT 	
                        ratee.""ParticipantNameFirst"" 	AS ""RateeFirstName"", 
                        ratee.""ParticipantNameLast"" 	AS ""RateeLastName"", 
                        vEC.""ParticipantRoleName""		AS ""RaterRole"", 
                        rater.""ParticipantNameFirst""	AS ""RaterFirstName"", 
                        rater.""ParticipantNameLast""	AS ""RaterLastName"", 
                        rater.""ParticipantEmail""		AS ""RaterEmail"",
                        rater.""ParticipantId""         AS ""RaterParticipantId"",
                        vEC.""DueDate""					AS ""DueDate"",
                        vEC.""EvaluationRateeId""       AS ""EvaluationRateeId"",
                        vEC.""EvaluationTestEmails""    AS ""EvaluationTestEmails"",
                        c.""ClientName""				AS ""ClientName"",
                        c.""ClientId""					AS ""ClientId"",
                        c.""ClientHelpContact""			AS ""ClientHelpContact"",
                        'Invitation - Rater'			AS ""MessageToSend""
                FROM 		""vwEvaluationsToComplete"" vEC
                INNER JOIN 	""Participant"" ratee
                ON			vEC.""ParticipantId"" = ratee.""ParticipantId""
                INNER JOIN 	""Participant"" rater
                ON			vEC.""Rater"" = rater.""ParticipantId""
                INNER JOIN	""Client"" c
                ON			ratee.""ClientId"" = c.""ClientId""
                WHERE 		""EvaluationParticipantDateLastEmailed"" IS NULL
                AND         vEC.""ParticipantRoleName"" != 'Self'
                AND 		rater.""ParticipantIsActive"" = TRUE;");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW public.""vwRateeInvitations"" AS
                SELECT 	
                        ratee.""ParticipantNameFirst"" 	AS ""RateeFirstName"", 
                        ratee.""ParticipantNameLast"" 	AS ""RateeLastName"", 
                        vEC.""ParticipantRoleName""		AS ""RaterRole"", 
                        rater.""ParticipantNameFirst""	AS ""RaterFirstName"", 
                        rater.""ParticipantNameLast""	AS ""RaterLastName"", 
                        rater.""ParticipantEmail""		AS ""RaterEmail"",
                        rater.""ParticipantId""         AS ""RaterParticipantId"",
                        vEC.""DueDate""					AS ""DueDate"",
                        vEC.""EvaluationRateeId""       AS ""EvaluationRateeId"",
                        vEC.""EvaluationTestEmails""    AS ""EvaluationTestEmails"",
                        c.""ClientName""				AS ""ClientName"",
                        c.""ClientId""					AS ""ClientId"",
                        c.""ClientHelpContact""			AS ""ClientHelpContact"",
                        'Invitation - Ratee'			AS ""MessageToSend""
                FROM 		""vwEvaluationsToComplete"" vEC
                INNER JOIN 	""Participant"" ratee
                ON			vEC.""ParticipantId"" = ratee.""ParticipantId""
                INNER JOIN 	""Participant"" rater
                ON			vEC.""Rater"" = rater.""ParticipantId""
                INNER JOIN	""Client"" c
                ON			ratee.""ClientId"" = c.""ClientId""
                WHERE 		""EvaluationParticipantDateLastEmailed"" IS NULL
                AND         vEC.""ParticipantRoleName"" = 'Self'
                AND 		rater.""ParticipantIsActive"" = TRUE;");


            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION
                public.""fnEligibleRaters"" (EvaluationRateeId uuid)
                RETURNS TABLE ( ""EvaluationRateeId"" uuid,
                                ""ParticipantId"" uuid, 
                                ""ParticipantNameFirst"" VARCHAR, 
                                ""ParticipantNameLast"" VARCHAR,
                                ""ParticipantEmail"" VARCHAR, 
                                ""ParticipantHasRegistered"" BOOLEAN)
                AS $func$
                WITH ep2 AS (
                    SELECT      p2.""ParticipantId""
                    FROM        public.""Participant"" p2
                    INNER JOIN  public.""EvaluationParticipant"" ep
                    ON          p2.""ParticipantId"" = ep.""ParticipantId""
                    AND         ep.""EvaluationRateeId"" = EvaluationRateeId
                )
                SELECT 
                            er.""EvaluationRateeId"",
                            p.""ParticipantId"",
                            p.""ParticipantNameFirst"",
                            p.""ParticipantNameLast"",
                            p.""ParticipantEmail"",
                            p.""ParticipantHasRegistered""
                FROM        public.""EvaluationRatee"" er
                INNER JOIN  public.""Participant"" p2
                ON          er.""ParticipantId"" = p2.""ParticipantId""
                AND         er.""EvaluationRateeId"" = EvaluationRateeId
                INNER JOIN  public.""Participant"" p
                ON          p2.""ClientId"" = p.""ClientId""
                LEFT JOIN   ep2
                ON          p.""ParticipantId"" = ep2.""ParticipantId""
                WHERE       p.""ParticipantIsActive"" = TRUE
                AND         ep2.""ParticipantId"" IS NULL
                ORDER BY    p.""ParticipantNameLast"", 
                            p.""ParticipantNameFirst""
                $func$
                LANGUAGE sql;");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"exec('DROP FUNCTION public.""fnEligibleRaters""');");
            
            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwDefaultEvaluationByParticipant""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwParticipant""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwReminders""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwEvaluationsByRatee""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwAdminRatees""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwEvaluationsToComplete""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwRatersByRatee""');");

            migrationBuilder.Sql(@"exec('DROP VIEW public.""vwAdminParticipants""');");
        }
    }
}
