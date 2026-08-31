using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuizApp
{
    public class User
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string BirthDate { get; set; }
    }

    public class Question
    {
        public string Text { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public List<int> CorrectOptionIndices { get; set; } = new List<int>(); // Индексы правильных ответов (начиная с 0)
    }

    public class Quiz
    {
        public string Title { get; set; }
        public List<Question> Questions { get; set; } = new List<Question>();
    }

    public class QuizResult
    {
        public string UserLogin { get; set; }
        public string QuizTitle { get; set; }
        public int Score { get; set; }
    }

    class Program
    {
        static string usersFile = "users.json";
        static string quizzesFile = "quizzes.json";
        static string resultsFile = "results.json";

        static List<User> users = new List<User>();
        static List<Quiz> quizzes = new List<Quiz>();
        static List<QuizResult> results = new List<QuizResult>();

        static User currentUser = null;

        static void Main(string[] args)
        {
            LoadData();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== ВІКТОРИНА ===");
                Console.WriteLine("1. Вхід");
                Console.WriteLine("2. Реєстрація");
                Console.WriteLine("3. Утиліта редагування вікторин (Admin)");
                Console.WriteLine("0. Вихід");
                Console.Write("Оберіть: ");

                switch (Console.ReadLine())
                {
                    case "1": Login(); break;
                    case "2": Register(); break;
                    case "3": AdminPanel(); break;
                    case "0": return;
                }
            }
        }

        static void Register()
        {
            Console.Clear();
            Console.Write("Логін: ");
            string login = Console.ReadLine();
            
            if (users.Any(u => u.Login == login))
            {
                Console.WriteLine("Цей логін вже зайнятий!");
                Console.ReadLine(); return;
            }

            Console.Write("Пароль: ");
            string pass = Console.ReadLine();
            Console.Write("Дата народження: ");
            string dob = Console.ReadLine();

            users.Add(new User { Login = login, Password = pass, BirthDate = dob });
            SaveData();
            Console.WriteLine("Реєстрація успішна!");
            Console.ReadLine();
        }

        static void Login()
        {
            Console.Clear();
            Console.Write("Логін: ");
            string login = Console.ReadLine();
            Console.Write("Пароль: ");
            string pass = Console.ReadLine();

            currentUser = users.FirstOrDefault(u => u.Login == login && u.Password == pass);

            if (currentUser != null) UserMenu();
            else
            {
                Console.WriteLine("Невірний логін або пароль!");
                Console.ReadLine();
            }
        }

        static void UserMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Привіт, {currentUser.Login}!");
                Console.WriteLine("1. Стартувати нову вікторину");
                Console.WriteLine("2. Мої минулі результати");
                Console.WriteLine("3. Топ-20 з вікторини");
                Console.WriteLine("4. Налаштування (змінити пароль/дату)");
                Console.WriteLine("0. Вихід з акаунту");
                Console.Write("Оберіть: ");

                switch (Console.ReadLine())
                {
                    case "1": StartQuiz(); break;
                    case "2": ShowMyResults(); break;
                    case "3": ShowTop20(); break;
                    case "4": Settings(); break;
                    case "0": currentUser = null; return;
                }
            }
        }

        static void StartQuiz()
        {
            Console.Clear();
            if (quizzes.Count == 0)
            {
                Console.WriteLine("Немає доступних вікторин.");
                Console.ReadLine(); return;
            }

            Console.WriteLine("Доступні вікторини:");
            for (int i = 0; i < quizzes.Count; i++)
                Console.WriteLine($"{i + 1}. {quizzes[i].Title}");
            Console.WriteLine($"{quizzes.Count + 1}. Змішана вікторина (Рандом)");

            Console.Write("Оберіть номер: ");
            if (int.TryParse(Console.ReadLine(), out int choice))
            {
                List<Question> selectedQuestions = new List<Question>();
                string quizTitle = "";

                if (choice > 0 && choice <= quizzes.Count)
                {
                    quizTitle = quizzes[choice - 1].Title;
                    selectedQuestions = quizzes[choice - 1].Questions.Take(20).ToList();
                }
                else if (choice == quizzes.Count + 1)
                {
                    quizTitle = "Змішана вікторина";
                    var allQuestions = quizzes.SelectMany(q => q.Questions).OrderBy(x => Guid.NewGuid()).ToList();
                    selectedQuestions = allQuestions.Take(20).ToList();
                }
                else return;

                if (selectedQuestions.Count == 0) return;

                int score = 0;
                foreach (var q in selectedQuestions)
                {
                    Console.Clear();
                    Console.WriteLine(q.Text);
                    for (int i = 0; i < q.Options.Count; i++)
                        Console.WriteLine($"{i + 1}. {q.Options[i]}");
                    
                    Console.WriteLine("Введіть номери правильних відповідей через кому (наприклад: 1,3): ");
                    string answer = Console.ReadLine();
                    
                    // Парсим ответы пользователя
                    var userAnswers = answer.Split(',')
                                            .Select(a => a.Trim())
                                            .Where(a => int.TryParse(a, out _))
                                            .Select(a => int.Parse(a) - 1)
                                            .OrderBy(a => a).ToList();

                    var correctAnswers = q.CorrectOptionIndices.OrderBy(a => a).ToList();

                    // Проверяем, совпадает ли список ответов (порядок не важен)
                    if (userAnswers.SequenceEqual(correctAnswers))
                    {
                        score++;
                    }
                }

                Console.Clear();
                Console.WriteLine($"Вікторину завершено! Правильних відповідей: {score} з {selectedQuestions.Count}");
                
                results.Add(new QuizResult { UserLogin = currentUser.Login, QuizTitle = quizTitle, Score = score });
                SaveData();

                // Высчитываем место в таблице
                var leaderboard = results.Where(r => r.QuizTitle == quizTitle).OrderByDescending(r => r.Score).ToList();
                int place = leaderboard.FindIndex(r => r.UserLogin == currentUser.Login && r.Score == score) + 1;
                Console.WriteLine($"Ваше місце у таблиці лідерів: {place}");
                Console.ReadLine();
            }
        }

        static void ShowMyResults()
        {
            Console.Clear();
            var myResults = results.Where(r => r.UserLogin == currentUser.Login).ToList();
            foreach (var r in myResults)
                Console.WriteLine($"Вікторина: {r.QuizTitle} | Бал: {r.Score}");
            Console.ReadLine();
        }

        static void ShowTop20()
        {
            Console.Clear();
            Console.Write("Введіть назву вікторини: ");
            string title = Console.ReadLine();
            var top = results.Where(r => r.QuizTitle.ToLower() == title.ToLower())
                             .OrderByDescending(r => r.Score)
                             .Take(20).ToList();
            
            Console.WriteLine("--- ТОП 20 ---");
            for (int i = 0; i < top.Count; i++)
                Console.WriteLine($"{i + 1}. {top[i].UserLogin} - {top[i].Score} балів");
            Console.ReadLine();
        }

        static void Settings()
        {
            Console.Clear();
            Console.Write("Новий пароль: ");
            currentUser.Password = Console.ReadLine();
            Console.Write("Нова дата народження: ");
            currentUser.BirthDate = Console.ReadLine();
            SaveData();
            Console.WriteLine("Дані оновлено!");
            Console.ReadLine();
        }

        // --- УТИЛИТА РЕДАКТИРОВАНИЯ (АДМИНКА) ---
        static void AdminPanel()
        {
            Console.Clear();
            Console.WriteLine("Вхід в утиліту редагування");
            Console.Write("Логін (введіть admin): ");
            string login = Console.ReadLine();
            Console.Write("Пароль (введіть admin): ");
            string pass = Console.ReadLine();

            // Простая проверка для утилиты, чтобы не усложнять БД
            if (login != "admin" || pass != "admin")
            {
                Console.WriteLine("Відмовлено в доступі.");
                Console.ReadLine(); return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== РЕДАКТОР ВІКТОРИН ===");
                Console.WriteLine("1. Створити нову вікторину");
                Console.WriteLine("2. Додати питання до існуючої");
                Console.WriteLine("0. Вихід");
                Console.Write("Оберіть: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        Console.Write("Назва нової вікторини: ");
                        quizzes.Add(new Quiz { Title = Console.ReadLine() });
                        SaveData();
                        Console.WriteLine("Створено!");
                        Console.ReadLine();
                        break;
                    case "2":
                        if (quizzes.Count == 0) break;
                        for (int i = 0; i < quizzes.Count; i++) Console.WriteLine($"{i + 1}. {quizzes[i].Title}");
                        Console.Write("Оберіть вікторину: ");
                        if (int.TryParse(Console.ReadLine(), out int qId) && qId > 0 && qId <= quizzes.Count)
                        {
                            var quiz = quizzes[qId - 1];
                            var newQ = new Question();
                            Console.Write("Текст питання: ");
                            newQ.Text = Console.ReadLine();
                            
                            Console.Write("Скільки варіантів відповіді буде? ");
                            if (int.TryParse(Console.ReadLine(), out int optCount))
                            {
                                for (int i = 0; i < optCount; i++)
                                {
                                    Console.Write($"Варіант {i + 1}: ");
                                    newQ.Options.Add(Console.ReadLine());
                                }
                                Console.Write("Номери правильних відповідей (через кому): ");
                                string correct = Console.ReadLine();
                                newQ.CorrectOptionIndices = correct.Split(',')
                                                                   .Select(a => int.Parse(a.Trim()) - 1)
                                                                   .ToList();
                                quiz.Questions.Add(newQ);
                                SaveData();
                                Console.WriteLine("Питання додано!");
                            }
                        }
                        Console.ReadLine();
                        break;
                    case "0": return;
                }
            }
        }

        // --- СОХРАНЕНИЕ И ЗАГРУЗКА ДАННЫХ (JSON) ---
        static void SaveData()
        {
            File.WriteAllText(usersFile, JsonSerializer.Serialize(users));
            File.WriteAllText(quizzesFile, JsonSerializer.Serialize(quizzes));
            File.WriteAllText(resultsFile, JsonSerializer.Serialize(results));
        }

        static void LoadData()
        {
            if (File.Exists(usersFile)) users = JsonSerializer.Deserialize<List<User>>(File.ReadAllText(usersFile));
            if (File.Exists(quizzesFile)) quizzes = JsonSerializer.Deserialize<List<Quiz>>(File.ReadAllText(quizzesFile));
            if (File.Exists(resultsFile)) results = JsonSerializer.Deserialize<List<QuizResult>>(File.ReadAllText(resultsFile));
        }
    }
}