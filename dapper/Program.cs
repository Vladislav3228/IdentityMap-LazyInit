using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using MySql.Data.MySqlClient;

namespace MyLI
{
    class ObjectWatcher
    {
        protected static Dictionary<string, DomainModel> list = new Dictionary<string, DomainModel>();
        protected static void AddToList(DomainModel obj)
        {
            string key = obj.GetType().ToString() + "." + obj.GetID();
            if (!list.ContainsKey(key))
            {
                list.Add(key, obj);
            }
        }
        protected static DomainModel GetFromList(string key)
        {
            if (list.ContainsKey(key))
                return list[key];
            else
                return null;
        }
        protected static void DeleteFromList(string key)
        {
            if (list.ContainsKey(key))
            {
                list.Remove(key);
            }
        }
    }

    abstract class DomainModel : ObjectWatcher
    {
        protected MySqlConnection conn;
        protected MySqlCommand comm;
        protected MySqlDataReader reader;
        protected string tableName;
        protected int id;
        protected ArrayList res;
        protected DomainModel()
        {
            string connString = "Server=localhost;Port=3306;Database=mybase;Uid=root;password='';";
            conn = new MySqlConnection(connString);
            comm = conn.CreateCommand();
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        public string GetID()
        {
            return id.ToString();
        }
        protected abstract void FillObj();
        public abstract void GetInfo();
        public DomainModel GetById(int id)
        {
            res = new ArrayList();
            this.id = id;
            string key = this.GetType() + "." + id;
            if (!list.ContainsKey(key))
            {
                comm.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + tableName + "'";
                int cnt = Convert.ToInt32(comm.ExecuteScalar());
                comm.CommandText = "SELECT * FROM " + tableName + " WHERE id = " + id.ToString();
                reader = comm.ExecuteReader();
                reader.Read();
                int i = 1;
                while (i < cnt)
                {
                    res.Add(reader[i].ToString());
                    i += 1;
                }
                reader.Close();
                FillObj();
                ObjectWatcher.AddToList(this);
                return this;
            }
            else return ObjectWatcher.GetFromList(key);
        }
        public int Insert(ArrayList args)
        {
            int i = 0;
            string str = "INSERT INTO " + tableName + "(";
            ArrayList columns = GetColumns();
            for (int j = 1; j < columns.Count - 1; j++)
            {
                str += columns[j] + ", ";
            }
            str += columns[columns.Count - 1] + ") VALUES (";
            while (i < args.Count - 1)
            {
                str += "'" + args[i] + "', ";
                i += 1;
            }
            str += "'" + args[args.Count - 1] + "')";
            comm.CommandText = str;
            comm.ExecuteNonQuery();
            comm.CommandText = "SELECT LAST_INSERT_ID()";
            return Convert.ToInt32(comm.ExecuteScalar());
        }
        public void Delete(int id)
        {
            ObjectWatcher.DeleteFromList(tableName + "." + id);
            comm.CommandText = "DELETE FROM " + tableName + " WHERE id = " + id;
            comm.ExecuteNonQuery();
            if (this.id == id)
            {
                this.id = default(int);
                res = new ArrayList();
            }
            string key = tableName + "." + id;
            ObjectWatcher.DeleteFromList(key);

        }
        public void Update(ArrayList args)
        {
            string key = tableName + "." + args[0];
            if (list.ContainsKey(key))
            {
                int k = 0;
                while (k < list[key].res.Count)
                {
                    list[key].res[k] = args[k + 1];
                    k++;
                }
                list[key].FillObj();
            }
            ArrayList columns = GetColumns();
            int i = 1;
            string str = "UPDATE " + tableName + " SET ";
            while (i < args.Count - 1)
            {
                str += columns[i] + " = '" + args[i] + "', ";
                i += 1;
            }
            str += columns[args.Count - 1] + " = '" + args[args.Count - 1] + "'";
            str += " WHERE id = " + args[0].ToString();
            comm.CommandText = str;
            comm.ExecuteNonQuery();
            Console.WriteLine(str);
            Console.WriteLine("обновление прошло успешно!");


        }
        private ArrayList GetColumns()
        {
            comm.CommandText = "SHOW COLUMNS FROM " + tableName;
            reader = comm.ExecuteReader();
            ArrayList columns = new ArrayList();
            while (reader.Read())
            {
                columns.Add(reader[0].ToString());
            }
            reader.Close();
            return columns;
        }
    }

    class Groups : DomainModel
    {
        public Groups()
        {
            tableName = "groups";
        }
        private string title;
        protected override void FillObj()
        {
            title = res[0].ToString();
        }
        public Groups Load(int id)
        {
            GetById(id);
            return this;
        }

        public override void GetInfo()
        {
            Console.WriteLine("Группа под номером " + id + " называется " + title);
        }
    }

    class Students : DomainModel
    {
        public Students()
        {
            tableName = "students";
        }
        private string name;
        private string age;
        private string group_id;

        public Groups gr = null;
        public Groups GetGroup()
        {
            if (gr == null)
            {
                gr = new Groups().Load(Convert.ToInt32(group_id));
            }
            return gr;
        }

        protected override void FillObj()
        {
            name = res[0].ToString();
            age = res[1].ToString();
            group_id = res[2].ToString();
        }

        public override void GetInfo()
        {
            Console.WriteLine(id + "-му Студенту из группы " + group_id + " по имени " + name + " всего " + age + Years());
        }
        private string Years()
        {
            int age = Convert.ToInt32(this.age);
            if (age % 10 == 1) return " год";
            else
            {
                if (age % 10 >= 2 && age % 10 <= 4) return " года";
                else return " лет";
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            //Cats c = new Cats();
            //c.GetById(c.Insert(new ArrayList() { 8, "Kirito" }));
            Students stud = new Students();
            stud.GetById(2);
            stud.GetInfo();
            if(stud.gr==null)
            Console.WriteLine("1111");
            Groups grs = stud.GetGroup();
            grs.GetInfo();
            if (stud.gr == null)
                Console.WriteLine("1111");
            else
            {
                Console.WriteLine(stud.gr);
            }
        }
    }
}