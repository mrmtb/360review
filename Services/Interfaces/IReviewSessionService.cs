using reviews.Models;

namespace reviews.Services;

public interface IReviewSessionService
{
    void SetSessionData(SessionData sessionData);
    SessionData GetSessionData();
    void ClearSessionData();
}
