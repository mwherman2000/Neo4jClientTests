using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4jClientTest2
{
    class Program
    {
        private static IDriver _driver;

        public static void Main()
        {
            const string stmt0 = "RETURN \"Hello World\"";
            const string stmt1 = "CREATE path=(acme:Company {name:\"Acme Corporation\"})" +
                                " -[:owns]-> " +
                                "(tesla:Car {make: 'tesla', model: 'modelX'})" +
                                "RETURN path, acme, tesla, 7890, 1.2, ['a', 'b', 'c'] AS abc ";

            const string stmt3 = "MATCH path=(co:Company)-[r:owns]->(car:Car) RETURN path, co, r, car, " +
                           "5678, 1.4, timestamp() AS unixepoctime, ['a', 'b', 'c'] AS abc LIMIT 5";
            const string stmt4 = "MATCH (n:Car) RETURN n.make LIMIT 5";
            const string stmt5 = "MATCH (n:Car) RETURN 1234 LIMIT 5";
            const string stmt6 = "MATCH (n) RETURN DISTINCT labels(n)";
            const string stmt7 = "MATCH (n) RETURN DISTINCT labels(n), count(*) AS NumofNodes, " +
"avg(size(keys(n))) AS AvgNumOfPropPerNode, min(size(keys(n))) AS MinNumPropPerNode, max(size(keys(n))) AS MaxNumPropPerNode, " +
"avg(size((n)-[]-())) AS AvgNumOfRelationships, min(size((n)-[]-())) AS MinNumOfRelationships, max(size((n)-[]-())) AS MaxNumOfRelationships ";
            const string stmt8 = "CALL db.labels() YIELD label RETURN label, timestamp() AS unixepoctime";
            const string stmt9 = "RETURN ['a', 'b', 'c'] AS abc, ['d', 123, 456.5] AS def";
            const string stmt10 = "RETURN 1 <> 0";
            const string stmta = "CREATE (co:Company {name:\"Able Corporation\"}) RETURN co";
            const string stmtb = "CREATE (co:Company {name:\"Baker Corporation\"}) RETURN co";
            const string stmtc = "CREATE (co:Company {name:\"Charlie Corporation\"}) RETURN co";
            const string stmtd = "CREATE (co:Company {name:\"Delta Corporation\"}) RETURN co";

            const string stmtd0 = "MATCH (n) DETACH DELETE n RETURN Count(*)";
            const string stmtd1 = "MATCH (n:Car) DETACH DELETE n RETURN Count(*)";
            const string stmtd2 = "MATCH (n:Company) DETACH DELETE n RETURN Count(*)";

            //_driver = GraphDatabase.Driver("bolt://52.207.74.86:33912", AuthTokens.Basic("neo4j", "gun-november-trips"));
            _driver = GraphDatabase.Driver("bolt://localhost:7687");

            using (var session = _driver.Session())
            {
                IStatementResult resultRecords0 = session.Run(stmtd0);

                IStatementResult resultRecords = session.Run(stmt1);

                List<string> entityKeys = (List<string>)resultRecords.Keys; // variable names & constants in RETURN clause
                var recordList = resultRecords.ToList<IRecord>();

                foreach (IRecord record in recordList) // for each match in the Cypher statement
                {
                    ProcessRecord(record, entityKeys);
                }

                using (var tx = session.BeginTransaction())
                {
                    resultRecords = tx.Run(stmta);
                    resultRecords = tx.Run(stmtb);
                    resultRecords = tx.Run(stmtc);
                    resultRecords = tx.Run(stmtd);
                    tx.Success();
                }
            }
            _driver.Close();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        public void Dispose()
        {
            _driver?.Dispose();
        }

        private static void ProcessRecord(IRecord record, List<string> entityKeys)
        {
            Console.WriteLine();
            foreach (string entityKey in entityKeys) // foreach entity in the RETURN statement
            {
                Console.WriteLine("Entity Key: '" + entityKey + "'");

                object obj = record.Values[entityKey];
                string entityType = obj.GetType().ToString();
                Console.WriteLine("Entity Type: " + entityType);

                ProcessEntity(obj, entityType);
             }
        }

        private static void ProcessEntity(object obj, string entityType)
        {
            switch (entityType)
            {
                case "Neo4j.Driver.Internal.Path":
                    {
                        IPath pathEntity = (IPath)obj;

                        DumpNodeEntity("StartNode", pathEntity.Start);
                        foreach (INode n in pathEntity.Nodes)
                        {
                            DumpNodeEntity("PathNode", n);
                        }
                        foreach (IRelationship rel in pathEntity.Relationships)
                        {
                            DumpRelationshipEntity("PathRelationship", rel);
                        }
                        break;
                    }
                case "Neo4j.Driver.Internal.Relationship":
                    {
                        IRelationship relEntity = (IRelationship)obj;

                        DumpRelationshipEntity("Relationship", relEntity);
                        break;
                    }
                case "Neo4j.Driver.Internal.Node":
                    {
                        INode nodeEntity = (INode)obj;

                        DumpNodeEntity("SimpleNode", nodeEntity);
                        break;
                    }
                case "System.String":
                case "System.Double":
                case "System.Int64":
                case "System.Boolean":
                    {
                        object scalarEntity = obj;

                        Console.WriteLine(entityType + ": " + scalarEntity.ToString());
                        break;
                    }
                default:
                    {
                        if (entityType.StartsWith("System.Collections.Generic.List")) // e.g. list of strings e.g. labels(n)
                        {
                            List<object> listEntity = (List<object>)obj;
                            foreach (object element in listEntity)
                            {
                                ProcessEntity(element, element.GetType().ToString());
                            }
                        }
                        else
                        {
                            throw new NotImplementedException(entityType);
                        }
                        break;
                    }
            }
    }

        private static void DumpNodeEntity(string msg, INode node)
        {
            Console.WriteLine(msg);
            if (node == null)
            {
                Console.WriteLine("Null Node");
                return;
            }
            long id = node.Id;
            List<string> labels = node.Labels.ToList(); // e.g. :Car
            IReadOnlyDictionary<string, object> props = node.Properties; // e.g. 2 props make:tesla and model:modelX

            foreach (string ls in labels)
                Console.WriteLine(id.ToString() + "\tLabel:\t" + ls);
            foreach (string pk in props.Keys)
                Console.WriteLine(id.ToString() + "\tProperty:\t" + pk + "\t" + props[pk].ToString());
        }

        private static void DumpRelationshipEntity(string msg, IRelationship rel)
        {
            Console.WriteLine(msg);
            if (rel == null)
            {
                Console.WriteLine("Null Relationship");
                return;
            }
            long id = rel.Id;
            long startId = rel.StartNodeId;
            long endId = rel.EndNodeId;
            string type = rel.Type;
            Console.WriteLine("Ids: " + id.ToString() + " " + startId.ToString() + " " + type + " " + endId.ToString());

            IReadOnlyDictionary<string, object> props = rel.Properties;
            foreach (string pk in props.Keys)
                Console.WriteLine(id.ToString() + "\tProperty:\t" + pk + "\t" + props[pk].ToString());
        }
    }
}
