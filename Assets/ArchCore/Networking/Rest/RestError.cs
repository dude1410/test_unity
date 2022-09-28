namespace ArchCore.Networking.Rest
{
    public class RestError
    {
        public string exceptionMessage;
        public RestErrorType exceptionType;

        public RestError()
        {
        }

        public RestError(RestErrorType type, string exceptionMessage)
        {
            this.exceptionMessage = exceptionMessage;
            exceptionType = type;
        }

        public static implicit operator RestException(RestError error)
        {
            return new RestException(error);
        }
    }

    public enum RestErrorType
    {
        Unsupported = 0,
        ExistingMail = 1,
        ExistingLogin = 2,
        SomethingWentWrong = 3,
        NotExistingUserOrInvalidPassword = 4,
        UnregisteredMail = 5,
        NotEnoughResources = 8,
        UnknownIdentifier = 9,
        MaxLevelReached = 10,

        UnavailableAction = 11
        //...
    }
}