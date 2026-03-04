-- Add columns to Evaluation Participant
ALTER TABLE public."EvaluationParticipant" 
  ADD COLUMN IF NOT EXISTS "SurveyLastSavedAt" timestamp with time zone,
  ADD COLUMN IF NOT EXISTS "SurveyTemplateKey" text,
  ADD COLUMN IF NOT EXISTS "SurveyTemplateVersion" integer;

-- Create EvaluationParticipantResponse table
CREATE TABLE IF NOT EXISTS public."EvaluationParticipantResponse" (
    "EvaluationParticipantResponseId" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EvaluationRateeId" uuid NOT NULL,
    "RaterParticipantId" uuid NOT NULL,
    "EvaluationParticipantSurveyPassword" character varying(255) NOT NULL,
    "SurveyTemplateKey" character varying(100) NOT NULL,
    "SurveyTemplateVersion" integer NOT NULL,
    "DraftJson" jsonb,
    "FinalJson" jsonb,
    "LastSavedAt" timestamp with time zone,
    "SubmittedAt" timestamp with time zone,
    "IsSubmitted" boolean NOT NULL DEFAULT false
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_EvaluationParticipantResponse_RateeId" 
  ON public."EvaluationParticipantResponse"("EvaluationRateeId");

CREATE INDEX IF NOT EXISTS "IX_EvaluationParticipantResponse_RaterParticipantId" 
  ON public."EvaluationParticipantResponse"("RaterParticipantId");

CREATE INDEX IF NOT EXISTS "IX_EvaluationParticipantResponse_Template" 
  ON public."EvaluationParticipantResponse"("SurveyTemplateKey", "SurveyTemplateVersion");

CREATE UNIQUE INDEX IF NOT EXISTS "UK_EvaluationParticipantResponse_Password" 
  ON public."EvaluationParticipantResponse"("EvaluationParticipantSurveyPassword");

-- Add foreign keys
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_EvaluationParticipantResponse_EvaluationRatee_EvaluationRat~') THEN
        ALTER TABLE public."EvaluationParticipantResponse"
        ADD CONSTRAINT "FK_EvaluationParticipantResponse_EvaluationRatee_EvaluationRat~"
        FOREIGN KEY ("EvaluationRateeId") REFERENCES public."EvaluationRatee"("EvaluationRateeId") ON DELETE RESTRICT;
    END IF;
END
$$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_EvaluationParticipantResponse_Participant_RaterParticipantId') THEN
        ALTER TABLE public."EvaluationParticipantResponse"
        ADD CONSTRAINT "FK_EvaluationParticipantResponse_Participant_RaterParticipantId"
        FOREIGN KEY ("RaterParticipantId") REFERENCES public."Participant"("ParticipantId") ON DELETE RESTRICT;
    END IF;
END
$$;

-- Update migration history
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260304065955_AddSurveyJSSupport', '10.0.3')
ON CONFLICT DO NOTHING;
