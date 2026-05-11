using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.ModelLibrary.Models;
using NotificationApp.ModelLibrary.Exceptions;
using NotificationApp.FEApplication.Validators;

namespace NotificationApp.FEApplication{
	public class UserApp{

		private IUserInteract userInteract;
		private  MobileNumberValidator mobileValidator;
		private  EmailValidator emailValidator;

        private  NameValidator nameValidator;

		public UserApp(IUserInteract userInteract){
			this.userInteract = userInteract;
			mobileValidator = new MobileNumberValidator();
			emailValidator = new EmailValidator();
			nameValidator = new NameValidator();
		}

		public void CreateUser(){
            string emailId = emailValidator.GetAndValidateEmail("Enter Email ID:");
            string mobileNumber = mobileValidator.GetAndValidateMobileNumber("Enter Mobile Number:");
            string name = nameValidator.GetUserInputForName("Enter Name:");
			User newUser = userInteract.CreateUser(userName:name, emailId:emailId, mobileNumber:mobileNumber);
			Console.WriteLine(newUser);
		}
    

		public void GetUserDetails(){
            string mobileNumber = mobileValidator.GetAndValidateMobileNumber("Enter Mobile Number you need to search:");
			User userDetails = userInteract.getUserByMobileNumber(mobileNumber);
			if(userDetails!=null){
				Console.WriteLine(userDetails);
			}else{
				Console.WriteLine("User not found.");
			}
		}

		public void UpdateUserDetails(){
            string mobileNumber = mobileValidator.GetAndValidateMobileNumber("Enter Mobile Number of the user you want to update:");
            string newName = nameValidator.GetUserInputForName("Enter New Name:");
            string newEmailId = emailValidator.GetAndValidateEmail("Enter New Email ID:");
            string newMobileNumber = mobileValidator.GetAndValidateMobileNumber("Enter New Mobile Number:");
			User updatedUser = userInteract.UpdateUser( mobileNumber:mobileNumber, newUserName:newName, newEmailId:newEmailId, newMobileNumber:newMobileNumber);
			if(updatedUser!=null){
				Console.WriteLine(updatedUser);
			}else{
				Console.WriteLine("User not found.");
			}
		}

		public void DeleteUser(){
            string mobileNumber = mobileValidator.GetAndValidateMobileNumber("Enter Mobile Number of the user you want to delete:");

			User deletedUser = userInteract.DeleteUser(mobileNumber);
			if(deletedUser!=null){
			   Console.WriteLine(deletedUser);
			}else{
				Console.WriteLine("User not found.");
			}
		}
	}
}
