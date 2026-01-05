using Microsoft.AspNetCore.Http;
using reviews.Models;

namespace reviews.Services
{
    public class ReviewSessionService : IReviewSessionService
    {
        private const string SessionKey = "SessionData";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReviewSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public void SetSessionData(SessionData sessionData)
        {
            if (_httpContextAccessor.HttpContext == null)
                throw new Exception("HttpContext is null");

            if (_httpContextAccessor.HttpContext.Session == null)
                throw new Exception("Session is null");

            Session.SetObject(SessionKey, sessionData);
        }

        public SessionData GetSessionData()
        {
            var sessionData = Session.GetObject<SessionData>(SessionKey);
            return sessionData ?? new SessionData();
        }

        public void ClearSessionData()
        {
            Session.Remove(SessionKey);
        }
    }
}