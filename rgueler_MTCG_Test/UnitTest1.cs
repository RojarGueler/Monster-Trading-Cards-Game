using Npgsql;
using NUnit.Framework;
using rgueler_mtcg.Database;
using rgueler_mtcg.GameObjects;

namespace rgueler_MTCG_Test
{
    public class Tests
    {
        private DBRepository dbRepository;
        private string connectionString = "Host=localhost;Username=postgres;Password=Pass2020!;Database=MTCG_DB";
        [SetUp]
        public void Setup()
        {
            dbRepository = new DBRepository();
            dbRepository.InitDB();
        }

        [Test]
        public void TestUserLogin()
        {
            User user1 = new User { Username = "testuser1", Password = "pw12345678" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser(); 
            bool loginResult = dbRepository.IsUserLoggedIn(user1.Username, user1.Password);
            Assert.IsTrue(loginResult, "User login failed.");
        }

        [Test]
        public void TestAddingCoins()
        {
            User user1 = new User { Username = "testuser5", Password = "pw12345638" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser(); 
            int result = dbRepository.GetCoins(user1.Username);

            Assert.AreEqual(20, result);
        }

        [Test]
        public void GetCoins_NonExistingUser_ReturnsMinusOne()
        {
            string nonExistingUsername = "non_existing_user";

            int result = dbRepository.GetCoins(nonExistingUsername);

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void GetUserName_ValidToken_ReturnsCorrectUsername()
        {
            User user1 = new User { Username = "testuser9", Password = "pw1bisb5638" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser(); 

            string validToken = "testuser9-mtcgToken";

            string result = dbRepository.GetUserName(validToken);

            Assert.AreEqual("testuser9", result);
        }

        [Test]
        public void GetUserName_TokenWithoutHyphen_ReturnsEmptyString()
        {
            User user1 = new User { Username = "testuser9", Password = "pw1bisb5638" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser(); 

            string tokenWithoutHyphen = "-mtcgToken";

            string result = dbRepository.GetUserName(tokenWithoutHyphen);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void GetUserName_EmptyToken_ReturnsEmptyString()
        {
            string emptyToken = "";

            string result = dbRepository.GetUserName(emptyToken);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void CreatePackageID_ShouldReturnUniqueIDs()
        {
            int packageID1 = dbRepository.CreatePackageID();
            int packageID2 = dbRepository.CreatePackageID();

            Assert.That(packageID1, Is.Not.EqualTo(packageID2));
        }

        [Test]
        public void IsAnyPackageAvailable_WithAvailablePackages_ShouldReturnPackageList()
        {
            int initialPackageCount = GetPackageCount();
            dbRepository.AddPackage(new Package { PackageId = 1, Bought = false });

            List<int> packageList = dbRepository.IsAnyPackageAvailable();

            Assert.That(packageList, Is.Not.Empty);
        }

        private int GetPackageCount()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT COUNT(*) FROM packages;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    int packageCount = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                    return packageCount;
                }
            }
        }

        [Test]
        public void DoesTokenExist_WithValidToken_ShouldReturnTrue()
        {
            User user1 = new User { Username = "user1", Password = "pw1234" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser();
            string token = user1.Username + "-mtcgToken";

            bool result = dbRepository.DoesTokenExist(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void GetUserCardsJSON_ShouldNotReturnValidJSON()
        {
            User user1 = new User { Username = "user2", Password = "pw123" };
            DBRepository dbRepository = new DBRepository(user1);
            
            dbRepository.AddUser();
            dbRepository.AddPackage(new Package { PackageId = 1, Bought = true });
            dbRepository.AcquirePackage(1, user1.Username);

            string result = dbRepository.GetUserCardsJSON(user1.Username + "-mtcgToken");

            Assert.IsNotNull(result);
            Assert.IsFalse(result.StartsWith("[{"));
        }

        [Test]
        public void DoesTokenExist_ExistingToken_ReturnsTrue()
        {
            User user1 = new User { Username = "testtest", Password = "testtest123" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser();

            string existingToken = user1.Username + "-mtcgToken";

            bool result = dbRepository.DoesTokenExist(existingToken);

            Assert.IsTrue(result);
        }

        [Test]
        public void DoesTokenExist_NonExistingToken_ReturnsFalse()
        {
            string nonExistingToken = "non_existing_token";

            bool result = dbRepository.DoesTokenExist(nonExistingToken);

            Assert.IsFalse(result);
        }

        [Test]
        public void DoesTokenExist_NullToken_ReturnsFalse()
        {
            string nullToken = null;

            bool result = dbRepository.DoesTokenExist(nullToken);

            Assert.IsFalse(result);
        }

        [Test]
        public void DoesTokenExist_EmptyToken_ReturnsFalse()
        {
            string emptyToken = "";

            bool result = dbRepository.DoesTokenExist(emptyToken);

            Assert.IsFalse(result);
        }

        [Test]
        public void DoesUserExist_ExistingUser_ReturnsTrue()
        {
            User user1 = new User { Username = "testtesttest", Password = "testtesttest123" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser();

            string existingUsername = user1.Username;

            bool result = dbRepository.DoesUserExist(existingUsername);

            Assert.IsTrue(result);
        }

        [Test]
        public void DoesUserExist_NonExistingUser_ReturnsFalse()
        {
            
            string nonExistingUsername = "non_existing_user";

            bool result = dbRepository.DoesUserExist(nonExistingUsername);

            Assert.IsFalse(result);
        }

        [Test]
        public void DoesUserExist_EmptyUsername_ReturnsFalse()
        {
            string emptyUsername = "";

            bool result = dbRepository.DoesUserExist(emptyUsername);

            Assert.IsFalse(result);
        }

        [Test]
        public void ValidUserToken_ValidUsernameAndToken_ReturnsTrue()
        {
            User user1 = new User { Username = "moinchen", Password = "moinchen123" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser();

            string validUsername = user1.Username;
            string validToken = validUsername + "-mtcgToken";

            bool result = dbRepository.ValidUserToken(validUsername, validToken);

            Assert.IsTrue(result);
        }

        [Test]
        public void ValidUserToken_InvalidUsername_ReturnsFalse()
        {
            string invalidUsername = "invalid_user";
            string validToken = invalidUsername + "-mtcgToken";

            bool result = dbRepository.ValidUserToken(invalidUsername, validToken);

            Assert.IsFalse(result);
        }

        [Test]
        public void ValidUserToken_InvalidToken_ReturnsFalse()
        {
            User user1 = new User { Username = "moinchen", Password = "moinchen123" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser();

            string validUsername = "valid_user";
            string invalidToken = validUsername + "invalid_token";

            bool result = dbRepository.ValidUserToken(validUsername, invalidToken);

            Assert.IsFalse(result);
        }

        [Test]
        public void ValidUserToken_NullToken_ReturnsFalse()
        {
            User user1 = new User { Username = "moinchen", Password = "moinchen123" };
            DBRepository dbRepository = new DBRepository(user1);
            dbRepository.AddUser();

            string validUsername = "valid_user";
            string nullToken = validUsername+null;

            bool result = dbRepository.ValidUserToken(validUsername, nullToken);

            Assert.IsFalse(result);
        }

        [Test]
        public void ValidUserToken_EmptyUsername_ReturnsFalse()
        {
            string emptyUsername = "";
            string validToken = emptyUsername + "-mtcgToken";

            bool result = dbRepository.ValidUserToken(emptyUsername, validToken);

            Assert.IsFalse(result);
        }
    }
}