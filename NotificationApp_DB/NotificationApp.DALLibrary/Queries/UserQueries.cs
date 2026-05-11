namespace NotificationApp.DALLibrary.Queries{
        public class UserQueries{
            public const string InsertUserQuery = "INSERT INTO users (user_name, mobile_number, email_id) VALUES (@userName, @mobileNumber, @emailId) RETURNING user_id";
            public const string GetAllUsersQuery = "Select * from users";
            public const string UpdateUserQuery = "Update users set user_name=@userName, mobile_number=@mobileNumber, email_id=@emailId where user_id=@key";
            public const string DeleteUserQuery = "Delete from users where user_id=@key";
            public const string GetUserByEmailQuery = "Select * from users where email_id=@email";
            public const string GetUserByMobileQuery = "Select * from users where mobile_number=@mobile";
            public const string GetUserByIdQuery = "Select * from users where user_id=@key";

        }
}