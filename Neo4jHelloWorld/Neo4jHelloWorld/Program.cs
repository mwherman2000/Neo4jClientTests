using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4jHelloWorld
{
    class Program
    {
        private static IDriver _driver;

        public static void Main()
        {
            _driver = GraphDatabase.Driver("bolt://52.207.74.86:33912", AuthTokens.Basic("neo4j", "gun-november-trips"));

            using (var session = _driver.Session())
            {
                string stmt0 = "RETURN \"Hello World\"";

                IStatementResult resultRecords = session.Run(stmt0);

                var recordList = resultRecords.ToList<IRecord>();
                IRecord record = recordList[0];
                IReadOnlyDictionary<string, object> entities = record.Values;
                KeyValuePair<string, object> entity = entities.First();
                string key = entity.Key;
                string value = entity.Value.ToString();
                Console.WriteLine("key: " + key + "\nvalue: " + value);

            }
            _driver.Close();
            Console.ReadLine();
        }
    }
}
