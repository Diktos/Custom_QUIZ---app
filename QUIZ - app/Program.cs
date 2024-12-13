using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace QUIZ___app
{
    public enum TopicQuiz // Тема вікторини
    {
        History,
        Geography,
        Biology,
        Assorted
    }
    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; } 
        public Tuple<int, int, int> DateOfBirth;
        public List<Tuple<string, int, int>> PassedQuizes = new List<Tuple<string, int, int>>();
        public User(string name, string password, int day, int month, int year)
        {
            Name = name;
            Password = password;
            DateOfBirth = new Tuple<int, int, int>(day, month, year);
            if (UsersOfProgram.Users == null)
            {
                UsersOfProgram.Users = new List<User>();
            }
        }
        public bool IsPasswordCorrect(string password)
        {
            if (Password == password) { return true; }
            return false;
        }
        public void PassingTheQuiz(Quiz selectedQuiz)
        {
            if (selectedQuiz == null)
            {
                Console.WriteLine("Selected quiz is empty. Please select a valid topic quiz");
                return;
            }
            int score = 0;
            for (int i = 0; i < selectedQuiz.Questions.Count(); i++)
            {
                Console.Clear();
                selectedQuiz.Questions[i].ShowQuestion();
                Console.WriteLine("Enter the number of your answer(s) separated by a space (1 2 3):");
                string correctAnswer = Console.ReadLine();
                string[] answerArray = correctAnswer.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                List<int> ParsedNumbers = new List<int>();
                foreach (string numString in answerArray) // Для того щоб в подальшому не було помилки,якщо введуть символ
                {
                    if (int.TryParse(numString, out int num))
                    {
                        ParsedNumbers.Add(num);
                    }
                }
                if (selectedQuiz.Questions[i].CorrectAnswers.SequenceEqual(ParsedNumbers))
                {
                    Console.WriteLine("The answer is correct! +1 point");
                    score++;
                }
                else
                {
                    Console.WriteLine("The answer is incorrect! ;(");
                }
                Console.ReadLine();
            }
            selectedQuiz.ResultsOfPasses.Add(new Tuple<string, int>(Name, score)); // Добавляємо результати користувача у базу учасників вікторини
            PassedQuizes.Add(new Tuple<string, int, int>(selectedQuiz.NameQuiz, selectedQuiz.UniqueId, score)); // Додаємо інфу вікторини, яку він проходив для відображення історії його здач 
            selectedQuiz.ResultsOfPasses.OrderByDescending(s => s.Item2); // Просто відсортовуємо від max кількості набраних балів до min всіх учасників
            int userRank = selectedQuiz.ResultsOfPasses.FindIndex(s => s.Item1 == Name) + 1; // Знаходимо індекс користувача у списку результатів
            Console.WriteLine($"You have completed the quiz called {selectedQuiz.NameQuiz} with a score of {score}.You are in {userRank} place in the quiz ranking.Congratulations!");
        } // Метод для здачі і висвітлення результатів
        public void StartQuiz() // Метод для активації проходження вікторини
        {
            Console.WriteLine("Select the quiz topic: Biology, History, Geography, Assorted(all topics)");
            string topicInput = Console.ReadLine();
            if (Enum.TryParse(topicInput, out TopicQuiz selectedTopic))
            {
                Quiz selectedQuiz = new Quiz();
                if (selectedTopic == TopicQuiz.Assorted)
                {
                    selectedQuiz = QuizesOfProgram.CreatingAssortedQuiz();
                    PassingTheQuiz(selectedQuiz);
                }
                else
                {
                    selectedQuiz = QuizesOfProgram.SelectQuizByTopic(selectedTopic);
                    PassingTheQuiz(selectedQuiz);
                }
            }
        }
        public void ShowEffectiveness() // Метод для відображення результативності користувача у вікторинах 
        {
            if (PassedQuizes == null)
            {
                Console.WriteLine("No passed quizzes to show effectiveness");
                return;
            }
            Console.WriteLine("Passed Quizes Effectiveness:");
            foreach (var quizInfo in PassedQuizes)
            {
                Console.WriteLine($"Quiz {quizInfo.Item1} with {quizInfo.Item2} ID - {quizInfo.Item3} scores");
            }
        }
        public void ChangeSettings() // Змінити пароль або дату народження
        {
            Console.WriteLine("Write 'one' if you want to change your password or 'two' if your date of birth: ");
            string chosenAction = Console.ReadLine();
            if (chosenAction == "one")
            {
                Console.WriteLine("Enter your password for account");
                string newPassword = Console.ReadLine();
                Password = newPassword;
                Console.WriteLine("Password changed");
            }
            else if (chosenAction == "two")
            {
                Console.WriteLine("Enter your date of birth for account in parts using the Enter key(day..month..year..)");
                string newDay = Console.ReadLine(); string newMonth = Console.ReadLine(); string newYear = Console.ReadLine();
                DateOfBirth = Tuple.Create(int.Parse(newDay), int.Parse(newMonth), int.Parse(newYear));
                Console.WriteLine("Date of birth changed");
            }
            else
            {
                Console.WriteLine("Error: incorrect data to change settings");
            }
        }
    }
    public class QuestionOfQuiz // одне питання вікторини
    {
        public string Question { get; set; }
        public int[] CorrectAnswers; // 1 3 4 - приклад як має виглядати 
        public List<string> Answers = new List<string>();
        public void ShowQuestion()
        {
            Console.WriteLine(Question);
            int i = 1;
            foreach (string answer in Answers)
            {
                Console.WriteLine($"{i++}.{answer}");
            }
        }
    }
    // IEqualityComparer - інтерфейс, який використовується для визначення спеціального порівняльника, який може порівнювати два об'єкти на рівність та обчислювати хеш-код для об'єктів
    //public class QuestionsComparer : IEqualityComparer<QuestionOfQuiz> // Використовуватиметься,якщо треба буде згруповати об'єкти через Disticnt
    //{
    //    public bool Equals(QuestionOfQuiz x, QuestionOfQuiz y)
    //    {
    //        if (x == null && y == null)
    //        {
    //            return false;
    //        }
    //        else
    //        {
    //            if (x.Question==y.Question && x.Answers.SequenceEqual(y.Answers)) // SequenceEqual - для перевірки рівності послідовностей
    //            {
    //                return true;
    //            }
    //            else
    //            {
    //                return false;
    //            }
    //        }
    //    }
    //    public int GetHashCode(QuestionOfQuiz obj)
    //    {
    //        return base.GetHashCode();
    //    }
    //}
    public class Quiz
    {
        public int UniqueId { get; set; }
        public string NameQuiz { get; set; }
        public TopicQuiz Topic { get; set; }
        public List<QuestionOfQuiz> Questions = new List<QuestionOfQuiz>();
        public List<Tuple<string, int>> ResultsOfPasses = new List<Tuple<string, int>>();// login користувача і бали
        public void CreateQuestionWithAnswers()
        {
            QuestionOfQuiz questionOfQuiz = new QuestionOfQuiz();
            Console.WriteLine("Enter a question:");
            questionOfQuiz.Question = Console.ReadLine();
            string answer; // TODO Обов'язково прям 20 питань запитати
            string correctAnswer;
            Console.WriteLine("Write 'BB' to end the input of answer choices");
            int i = 1;
            while (true)
            {
                Console.WriteLine($"Enter your answer {i}:");
                answer = Console.ReadLine();
                if (answer == "BB") { i = 0; break; }
                questionOfQuiz.Answers.Add(answer);
                i++;
            }
            Console.WriteLine("Write the numbers of the correct answers separated by a space:"); // через пробіл
            correctAnswer = Console.ReadLine();
            string[] answerArray = correctAnswer.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            List<int> ParsedNumbers = new List<int>();
            foreach (string numString in answerArray) // Для того щоб в подальшому не було помилки,якщо введуть символ
            {
                if (int.TryParse(numString, out int num))
                {
                    ParsedNumbers.Add(num);
                }
            }
            questionOfQuiz.CorrectAnswers = ParsedNumbers.ToArray();

            Questions.Add(questionOfQuiz);
            Console.WriteLine("Question added");
        }
        public void AddQuestions()
        {
            for (int i = 0; i < 2; i++)
            {
                CreateQuestionWithAnswers();
            }
        }
    }
    public class UsersOfProgram
    {
        public static List<User> Users = new List<User>(); // Ліст всіх користувачів класу
        public static bool LoginExists(string login) // Проходимо по лісту юсерів і перевірямо чи доступний логін
        {
            if (Users == null)
            {
                return false;
            }
            foreach (var user in Users)
            {
                if (user.Name == login)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool SignIn(string login, string password)
        {
            foreach (var user in Users)
            {
                if (user.Name == login && user.IsPasswordCorrect(password)) // Для того, щоб не прирівняти до true пароль іншого користувача, тому і login теж перевіряємо
                {
                    Console.WriteLine("Login successfully completed!");
                    return true;
                }
            }
            Console.WriteLine("Password is incorrect");
            return false;
        }
        public static void SaveUsers(string filePath) // Закачуєм у файл
        {
            string json = JsonConvert.SerializeObject(Users);
            File.WriteAllText(filePath, json);
        }
        public static List<User> DownloadUsers(string filePath) // Качаєм
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<User>>(json);
            }
            else
            {
                Console.WriteLine("File is empty or does not exist");
                return new List<User>();
            }
        }
        public static User GetCurrentUserByLogin(string login)
        {
            foreach (var user in Users)
            {
                if (user.Name == login)
                {
                    return user;
                }
            }
            return null;
        }

    }
    public class QuizesOfProgram
    {
        public static int UniqueId { get; set; } = 0; // У кожної Quiz буде свій унікальний айді
        public static List<Quiz> Quizes = new List<Quiz>();
        public static void AddQuiz()
        {
            Quiz quiz = new Quiz();
            Console.WriteLine("Wtite name the quiz:");
            string namequiz = Console.ReadLine();
            quiz.NameQuiz = namequiz;
            Console.WriteLine("Select the quiz topic: Biology, History, Geography");
            string topic = Console.ReadLine();
            TopicQuiz QuizType;
            Enum.TryParse(topic, out QuizType); // перетворення у int
            quiz.Topic = QuizType;
            quiz.AddQuestions();
            if (Quizes == null)
            {
               Quizes=new List<Quiz>();
            }
            quiz.UniqueId = UniqueId; UniqueId++;
            Quizes.Add(quiz);
           
        }
        public static Quiz SelectQuizByTopic(TopicQuiz topic)
        {
            if(Quizes == null)
            {
                return null;
            }
            // Фільтруємо список вікторин за темою
            List<Quiz> filteredQuizes = new List<Quiz>();

            foreach (var quiz in Quizes)
            {
                if (quiz.Topic == topic)
                {
                    filteredQuizes.Add(quiz);
                }
            }
            if (filteredQuizes.Count == 0)
            {
                Console.WriteLine("There are no quizzes on this topic yet :(");
                return null;
            }
            // Вибираємо випадкову вікторину з відфільтрованого списку
            Random rnd = new Random();
            int index = rnd.Next(0, filteredQuizes.Count);
            return filteredQuizes[index];
        }
        public static Quiz CreatingAssortedQuiz()  // Створення ASORTI вікторини
        {
            // Перевірка, чи Quizes не є пустим або null
            if (Quizes == null || Quizes.Count == 0)
            {
                Console.WriteLine("No quizzes available to create assorted quiz");
                return null;
            }
            // Свторюємо новий об'єкт класу Quiz для заповнення його питаннями з інших вікторин
            Quiz assortedQuiz = new Quiz();
            assortedQuiz.Topic = TopicQuiz.Assorted;
            assortedQuiz.NameQuiz = "ASORTI QUIZ";
            List<QuestionOfQuiz> randomQuestions = new List<QuestionOfQuiz>();
            Random rnd = new Random();
            int attempts = 0; // Спроби створень унікальних питань з припустимим шансом 1/5,щоб уникнути вхід циклу у нескінченність
            int requiredQuestions = 20; // Необхідні питання

            while (randomQuestions.Count < requiredQuestions && attempts < 100) 
            {
                Quiz rndQuiz = GetQuizById(rnd.Next(0, Quizes.Count()));
                if (rndQuiz.Questions == null || rndQuiz.Questions.Count == 0)
                {
                    Console.WriteLine($"Quiz {rndQuiz.NameQuiz} does not have any questions");
                    continue;
                }
                QuestionOfQuiz rndQuestion = rndQuiz.Questions[rnd.Next(0, rndQuiz.Questions.Count)];
                if (randomQuestions.Contains(rndQuestion))
                {
                    Console.WriteLine("Duplicate question found. Skipping question");
                    continue;
                }
                randomQuestions.Add(rndQuestion);
                attempts++;
            }
            assortedQuiz.Questions = randomQuestions;
            return assortedQuiz;
        }
        public static void ShowEffectivenessOfSpecificQuiz() // Показати результативність вікторини вибраної користувачем(певної)
        {
            Console.WriteLine("Write the quiz ID to see its rating");
            int selectedQuizID;
            if (!int.TryParse(Console.ReadLine(), out selectedQuizID))
            {
                Console.WriteLine("Invalid quiz ID. Please enter a valid ID");
                return;
            }
            Quiz chooceQuiz = GetQuizById(selectedQuizID);
            if (chooceQuiz == null)
            {
                Console.WriteLine($"Quiz with {selectedQuizID} ID not found.");
                return;
            }
            Console.WriteLine($"Top 20 results for quiz {chooceQuiz.NameQuiz}:");
            var topResults = chooceQuiz.ResultsOfPasses.OrderByDescending(s => s.Item2).Take(20); // Take() - Кількість елементів, яку потрібно вибрати. OBD() - Групування з кінця до початку
            for (int pos = 1; pos <= topResults.Count(); pos++)
            {
                var result = topResults.ElementAt(pos - 1);
                Console.WriteLine($"{pos}. User: {result.Item1} with {result.Item2} score");
            }
        }
        public static void SaveQuizes(string filePath) // У файл закачуєм
        {
            string json = JsonConvert.SerializeObject(Quizes);
            File.WriteAllText(filePath, json);
        }
        public static List<Quiz> DownloadQuizes(string filePath) // З файла качаєм
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<Quiz>>(json);
            }
            else
            {
                Console.WriteLine("File is empty or does not exist");
                return new List<Quiz>();
            }
        }
        public static Quiz GetQuizById(int id)
        {
            foreach (var quiz in Quizes)
            {
                if (quiz.UniqueId == id)
                {
                    return quiz;
                }
            }
            return null;
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            string PATH_TO_USERS = Directory.GetCurrentDirectory() + "\\DataUsers.txt";
            string PATH_TO_QUIZES = Directory.GetCurrentDirectory() + "\\DataQuizes.txt";
            UsersOfProgram.Users = UsersOfProgram.DownloadUsers(PATH_TO_USERS);
            QuizesOfProgram.Quizes = QuizesOfProgram.DownloadQuizes(PATH_TO_QUIZES);
        AutMenu: Console.WriteLine("\t\t\tWelcome to our quiz!");
            Console.WriteLine("Write an autorization method:\nreg - registration\nlog - login\nanother team - close the application");
            string ChoiceOfAuthorizationMethod; // Вибір способу авторизації
            ChoiceOfAuthorizationMethod = Console.ReadLine();
            User currentUser = new User(" ", " ", 0, 0, 0);
            Console.OutputEncoding=System.Text.Encoding.UTF8;
            switch (ChoiceOfAuthorizationMethod)
            {
                case "reg":
                    string login;
                    string password;
                    string day; string month; string year;
                    Console.WriteLine("Enter your login for account");
                    do
                    {
                        login = Console.ReadLine();
                        if (UsersOfProgram.LoginExists(login)) { Console.WriteLine("This login is already taken. Please enter another one"); }
                    } while (UsersOfProgram.LoginExists(login)); // Вводимо поки не буде введений логін якого нема
                    Console.WriteLine("Enter your password for account");
                    password = Console.ReadLine();
                    Console.WriteLine("Enter your date of birth for account in parts(day..month..year..)");
                    day = Console.ReadLine(); month = Console.ReadLine(); year = Console.ReadLine();
                    if (UsersOfProgram.Users == null)
                    {
                        UsersOfProgram.Users = new List<User>();
                    }
                    UsersOfProgram.Users.Add(new User(login, password, int.Parse(day), int.Parse(month), int.Parse(year)));
                    currentUser = UsersOfProgram.GetCurrentUserByLogin(login);
                    goto User_menu;
                case "log":
                    Console.WriteLine("Enter your login to sign in");
                    do
                    {
                        login = Console.ReadLine();
                        if (!UsersOfProgram.LoginExists(login)) { Console.WriteLine("Enter your existing login"); }
                    } while (!UsersOfProgram.LoginExists(login));
                    Console.WriteLine("Enter your password to sign in");
                    do
                    {
                        password = Console.ReadLine();
                    } while (!UsersOfProgram.SignIn(login, password));
                    currentUser = UsersOfProgram.GetCurrentUserByLogin(login);
                    goto User_menu;
                default:
                    Console.WriteLine("Work is finished");
                    CustomPause();
                    return;
            }
        User_menu:
            while (true)
            {
                Console.ReadLine();
                Console.Clear();
                string ChoiceAction; // Вибір дії
                Console.WriteLine("\t\t\tMENU");
                Console.WriteLine("Write an program method:\ncreate - create quiz\ntake - taking the quiz\nresults - results of the passed quizzes\n" +
                    "rating - TOP 20 users rating in the certain quiz\nchangeset - changing settings user data\nautm - back to authorization menu\n" +
                    "exit - save and exit");
                ChoiceAction = Console.ReadLine();
                switch (ChoiceAction)
                {
                    case "create":
                        QuizesOfProgram.AddQuiz();
                        break;
                    case "take":
                        currentUser.StartQuiz();
                        break;
                    case "results":
                        currentUser.ShowEffectiveness();
                        break;
                    case "rating":
                        QuizesOfProgram.ShowEffectivenessOfSpecificQuiz();
                        break;
                    case "changeset":
                        currentUser.ChangeSettings();
                        break;
                    case "autm":
                        goto AutMenu;
                    case "exit":
                        UsersOfProgram.SaveUsers(PATH_TO_USERS);
                        QuizesOfProgram.SaveQuizes(PATH_TO_QUIZES);
                        CustomPause();
                        return;
                    default:
                        Console.WriteLine("Error command");
                        break;
                }
            }
        }
        static void CustomPause() // Домашня пауза 
        {
            Console.WriteLine("Write any key to continue");
            Console.ReadLine();
        }
    }
}