#nullable disable
using System;
using System.Collections.Generic;

namespace TaskTrackerApp
{
    public interface INotificationService
    {
        void Send(Notifications notice, Performer to);
    }

    public class EmailNotification : INotificationService
    {
        public void Send(Notifications notice, Performer to)
        {
            Console.WriteLine($"[Email]: Отправлено {to.Name}. Текст: {notice.TextComment?.Text}");
        }
    }

    public class TelegramBotNotification : INotificationService
    {
        public void Send(Notifications notice, Performer to)
        {
            Console.WriteLine($"[Telegram Bot]: Отправлено {to.Name}. Текст: {notice.TextComment?.Text}");
        }
    }

    public class Role
    {
        public string Name { get; set; }
        private string[] PermissionSet { get; set; }

        public Role(string name, string[] permissions)
        {
            Name = name;
            PermissionSet = permissions;
        }

        public bool HasPermission(string permission)
        {
            if (PermissionSet == null) return false;
            return Array.Exists(PermissionSet, p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class Performer
    {
        public string Name { get; set; }
        private string email;
        public Role Role { get; set; }

        public Performer(string name, string email, Role role)
        {
            Name = name;
            this.email = email;
            Role = role;
        }

        public Role GetRole() => Role;
        public string GetEmail() => email;
    }

    public class HistoryOfTransitions
    {
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public Performer Author { get; set; }

        public HistoryOfTransitions(string newStatus, Performer author)
        {
            OldStatus = "None";
            NewStatus = newStatus;
            Author = author;
        }
    }

    public class Comments
    {
        public string Text { get; set; }
        private string timeCreated;
        public Performer Author { get; set; }

        public Comments(string text, Performer author)
        {
            Text = text;
            Author = author;
            timeCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string GetTimeCreated() => timeCreated;
    }

    public class Notifications
    {
        public Comments TextComment { get; set; }
        private readonly INotificationService _notificationService;

        public Notifications(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public string Send(Performer performer, string message)
        {
            TextComment = new Comments(message, null);
            _notificationService.Send(this, performer);
            return "success_res";
        }
    }

    public class Task
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string CurStatus { get; set; }
        public string Deadline { get; set; }
        public bool Expired { get; set; }
        public List<Performer> Performers { get; set; } = new List<Performer>();
        public List<Comments> TaskComments { get; set; } = new List<Comments>();
        public List<HistoryOfTransitions> History { get; set; } = new List<HistoryOfTransitions>();
        private readonly INotificationService _notificationService;

        public Task(string title, string description, Performer author, string deadline, string status, INotificationService notificationService)
        {
            Name = title;
            Desc = description;
            CurStatus = status;
            Deadline = deadline;
            Expired = false;
            _notificationService = notificationService;
        }

        public bool AssignPerform(Performer candidate)
        {
            Role role = candidate.GetRole();
            bool access = role.HasPermission("ExecuteTask");
            if (access)
            {
                this.AddPerformer(candidate);
                foreach (var performer in Performers)
                {
                    Notifications notificate = new Notifications(_notificationService);
                    string res = notificate.Send(performer, $"К задаче {Name} добавлен новый участник: {candidate.Name}.");
                }
                return true;
            }
            return false;
        }

        private void AddPerformer(Performer candidate)
        {
            if (!Performers.Contains(candidate))
            {
                Performers.Add(candidate);
            }
        }

        public bool CreateComment(string text, Performer author)
        {
            bool isValid = ValidText(text);
            if (isValid)
            {
                Comments comment = new Comments(text, author);
                TaskComments.Add(comment);
                return true;
            }
            return false;
        }

        private bool ValidText(string text) => !string.IsNullOrWhiteSpace(text);
    }

    public class TaskController
    {
        private readonly INotificationService _notificationService;

        public TaskController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public bool CreateTask(string title, string description, Performer author, string deadline, string status)
        {
            Task newTask = new Task(title, description, author, deadline, status, _notificationService);
            HistoryOfTransitions hisOfTran = new HistoryOfTransitions(status, author);
            newTask.History.Add(hisOfTran);
            return true;
        }
    }

    class Program
    {
        static void Main()
        {
            Role developerRole = new Role("Разработчик", new string[] { "ExecuteTask" });
            Performer manager = new Performer("Алексей (Менеджер)", "alex@company.com", new Role("PM", new string[0]));
            Performer developerAlice = new Performer("Алиса (Разработчик)", "alice@company.com", developerRole);
            Performer developerBob = new Performer("Боб (Разработчик)", "bob@company.com", developerRole);

            INotificationService currentNotificationWay = new TelegramBotNotification();
            TaskController controller = new TaskController(currentNotificationWay);
            
            controller.CreateTask("Разработать модуль API", "Сделать интеграцию базы данных", manager, "2026-12-31", "New");

            Task currentTask = new Task("Разработать модуль API", "Сделать интеграцию базы данных", manager, "2026-12-31", "New", currentNotificationWay);
            currentTask.Performers.Add(developerAlice);

            Console.WriteLine("Попытка назначить Боба на задачу (Уведомления через Telegram)");
            currentTask.AssignPerform(developerBob);
        }
    }
}
