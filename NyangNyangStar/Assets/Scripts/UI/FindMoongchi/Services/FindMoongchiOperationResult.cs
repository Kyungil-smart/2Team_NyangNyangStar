namespace UI.FindMoongchi
{
    public readonly struct FindMoongchiOperationResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public int ErrorCode { get; }

        private FindMoongchiOperationResult(bool isSuccess, string message, int errorCode)
        {
            IsSuccess = isSuccess;
            Message = message;
            ErrorCode = errorCode;
        }

        public static FindMoongchiOperationResult Success(string message = null)
            => new FindMoongchiOperationResult(true, message, 0);

        public static FindMoongchiOperationResult Fail(string message, int errorCode = 0)
            => new FindMoongchiOperationResult(false, message, errorCode);
    }
}
