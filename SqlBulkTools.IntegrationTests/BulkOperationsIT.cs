﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using SqlBulkTools.Enumeration;
using SqlBulkTools.IntegrationTests.Data;
using SqlBulkTools.IntegrationTests.Model;
using TestContext = SqlBulkTools.IntegrationTests.Data.TestContext;

namespace SqlBulkTools.IntegrationTests
{
    [TestFixture]
    class BulkOperationsIT
    {
        private const int RepeatTimes = 1;

        private BookRandomizer _randomizer;
        private TestContext _db;
        private List<Book> _bookCollection;
        [OneTimeSetUp]
        public void Setup()
        {
            _db = new TestContext();
            _randomizer = new BookRandomizer();
            Database.SetInitializer(new DatabaseInitialiser());
            _db.Database.Initialize(true);
            FileHelper.DeleteLogFile();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [TestCase(1000)]
        public void SqlBulkTools_BulkInsert(int rows)
        {
            BulkDelete(_db.Books.ToList());
            _bookCollection = new List<Book>();
            _bookCollection.AddRange(_randomizer.GetRandomCollection(rows));
            List<long> results = new List<long>();

            FileHelper.AppendToLogFile("Testing BulkInsert with " + rows + " rows");

            for (int i = 0; i < RepeatTimes; i++)
            {
                long time = BulkInsert(_bookCollection);
                results.Add(time);
            }
            double avg = results.Average(l => l);
            FileHelper.AppendToLogFile("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

            Assert.AreEqual(rows * RepeatTimes, _db.Books.Count());
        }


        [TestCase(1000)]
        public void SqlBulkTools_BulkInsert_WithAllColumns(int rows)
        {
            BulkDelete(_db.Books.ToList());
            _bookCollection = new List<Book>();
            _bookCollection.AddRange(_randomizer.GetRandomCollection(rows));
            List<long> results = new List<long>();

            FileHelper.AppendToLogFile("Testing BulkInsert with " + rows + " rows");

            for (int i = 0; i < RepeatTimes; i++)
            {
                long time = BulkInsertAllColumns(_bookCollection);
                results.Add(time);
            }
            double avg = results.Average(l => l);
            FileHelper.AppendToLogFile("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

            Assert.AreEqual(rows * RepeatTimes, _db.Books.Count());
        }

        [TestCase(500, 500)]
        public void SqlBulkTools_BulkInsertOrUpdate(int rows, int newRows)
        {
            BulkDelete(_db.Books.ToList());
            var fixture = new Fixture();
            _bookCollection = _randomizer.GetRandomCollection(rows);

            List<long> results = new List<long>();

            FileHelper.AppendToLogFile("Testing BulkInsertOrUpdate with " + (rows + newRows) + " rows");

            for (int i = 0; i < RepeatTimes; i++)
            {
                BulkInsert(_bookCollection);

                // Update some rows
                for (int j = 0; j < 200; j++)
                {
                    var newBook = fixture.Build<Book>().Without(s => s.ISBN).Create();
                    var prevIsbn = _bookCollection[j].ISBN;
                    _bookCollection[j] = newBook;
                    _bookCollection[j].ISBN = prevIsbn;
                }

                // Add new rows
                _bookCollection.AddRange(_randomizer.GetRandomCollection(newRows));


                long time = BulkInsertOrUpdate(_bookCollection);
                results.Add(time);

                Assert.AreEqual(rows + newRows, _db.Books.Count());

            }

            double avg = results.Average(l => l);
            FileHelper.AppendToLogFile("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

        }


        [TestCase(500, 500)]
        public void SqlBulkTools_BulkInsertOrUpdateAllColumns(int rows, int newRows)
        {
            BulkDelete(_db.Books.ToList());
            var fixture = new Fixture();
            _bookCollection = _randomizer.GetRandomCollection(rows);

            List<long> results = new List<long>();

            FileHelper.AppendToLogFile("Testing BulkInsertOrUpdateAllColumns with " + (rows + newRows) + " rows");

            for (int i = 0; i < RepeatTimes; i++)
            {
                BulkInsert(_bookCollection);

                // Update some rows
                for (int j = 0; j < 200; j++)
                {
                    var newBook = fixture.Build<Book>().Without(s => s.ISBN).Create();
                    var prevIsbn = _bookCollection[j].ISBN;
                    _bookCollection[j] = newBook;
                    _bookCollection[j].ISBN = prevIsbn;
                }

                // Add new rows
                _bookCollection.AddRange(_randomizer.GetRandomCollection(newRows));


                long time = BulkInsertOrUpdateAllColumns(_bookCollection);
                results.Add(time);

                Assert.AreEqual(rows + newRows, _db.Books.Count());

            }

            double avg = results.Average(l => l);
            FileHelper.AppendToLogFile("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

        }


        [TestCase(1000)]
        public void SqlBulkTools_BulkUpdate(int rows)
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new PriceBuilder());
            fixture.Customizations.Add(new IsbnBuilder());
            fixture.Customizations.Add(new TitleBuilder());

            BulkDelete(_db.Books.ToList());

            List<long> results = new List<long>();

            FileHelper.AppendToLogFile("Testing BulkUpdate with " + rows + " rows");

            for (int i = 0; i < RepeatTimes; i++)
            {

                _bookCollection = _randomizer.GetRandomCollection(rows);
                BulkInsert(_bookCollection);

                // Update half the rows
                for (int j = 0; j < rows / 2; j++)
                {
                    var newBook = fixture.Build<Book>().Without(s => s.Id).Without(s => s.ISBN).Create();
                    var prevIsbn = _bookCollection[j].ISBN;
                    _bookCollection[j] = newBook;
                    _bookCollection[j].ISBN = prevIsbn;

                }

                long time = BulkUpdate(_bookCollection);
                results.Add(time);

                var testUpdate = _db.Books.FirstOrDefault();
                Assert.AreEqual(_bookCollection[0].Price, testUpdate.Price);
                Assert.AreEqual(_bookCollection[0].Title, testUpdate.Title);
                Assert.AreEqual(_db.Books.Count(), _bookCollection.Count);

                BulkDelete(_bookCollection);
            }
            double avg = results.Average(l => l);
            FileHelper.AppendToLogFile("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

        }


        [TestCase(1000)]
        public void SqlBulkTools_BulkUpdateOnIdentityColumn(int rows)
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new PriceBuilder());
            fixture.Customizations.Add(new IsbnBuilder());
            fixture.Customizations.Add(new TitleBuilder());
            BulkOperations bulk = new BulkOperations();

            BulkDelete(_db.Books.ToList());
            _bookCollection = _randomizer.GetRandomCollection(rows);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                    .ForCollection(_bookCollection)
                    .WithTable("Books")
                    .AddAllColumns()
                    .BulkInsert()
                    .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                    .Commit(conn);


                    // Update half the rows
                    for (int j = 0; j < rows / 2; j++)
                    {
                        var newBook = fixture.Build<Book>().Without(s => s.Id).Without(s => s.ISBN).Create();
                        var prevId = _bookCollection[j].Id;
                        _bookCollection[j] = newBook;
                        _bookCollection[j].Id = prevId;

                    }

                    bulk.Setup<Book>()
                        .ForCollection(_bookCollection)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkUpdate()
                        .MatchTargetOn(x => x.Id)
                        .SetIdentityColumn(x => x.Id)
                        .Commit(conn);
                }

                trans.Complete();
            }

            var testUpdate = _db.Books.FirstOrDefault();
            Assert.AreEqual(_bookCollection[0].Price, testUpdate.Price);
            Assert.AreEqual(_bookCollection[0].Title, testUpdate.Title);
            Assert.AreEqual(_db.Books.Count(), _bookCollection.Count);
        }

        [TestCase(1000)]
        public void SqlBulkTools_BulkDelete(int rows)
        {
            _bookCollection = _randomizer.GetRandomCollection(rows);
            BulkDelete(_db.Books.ToList());

            List<long> results = new List<long>();

            FileHelper.AppendToLogFile("Testing BulkDelete with " + rows + " rows");

            for (int i = 0; i < RepeatTimes; i++)
            {
                BulkInsert(_bookCollection);
                long time = BulkDelete(_bookCollection);
                results.Add(time);
                Assert.AreEqual(0, _db.Books.Count());
            }
            double avg = results.Average(l => l);
            FileHelper.AppendToLogFile("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

        }

        [Test]
        public void SqlBulkTools_IdentityColumnWhenNotSet_ThrowsIdentityException()
        {
            // Arrange
            var books = _db.Books.ToList();


            BulkDelete(books);

                
         _bookCollection = _randomizer.GetRandomCollection(20);
                
            
            BulkOperations bulk = new BulkOperations();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager
                .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
            {
                Assert.Throws<IdentityException>(() => bulk.Setup<Book>()
                            .ForCollection(_bookCollection)
                            .WithTable("Books")
                            .AddAllColumns()
                            .BulkUpdate()
                            .MatchTargetOn(x => x.Id)
                            .Commit(conn), "Cannot update identity " +
                             "column 'Id'. SQLBulkTools requires the SetIdentityColumn" +
                             " method to be configured if an identity column is being used. Please" +
                             " reconfigure your setup and try again.");
            }



        }

        [Test]
        public void SqlBulkTools_IdentityColumnSet_UpdatesTargetWhenSetIdentityColumn()
        {
            // Arrange
            var books = _db.Books.ToList();
            BulkDelete(books);
            BulkOperations bulk = new BulkOperations();
            _bookCollection = _randomizer.GetRandomCollection(20);
            string testDesc = "New Description";

            BulkInsert(_bookCollection);

            _bookCollection = _db.Books.ToList();
            _bookCollection.First().Description = testDesc;

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(_bookCollection)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkUpdate()
                        .SetIdentityColumn(x => x.Id)
                        .MatchTargetOn(x => x.Id)
                        .Commit(conn);
                }

                trans.Complete();
            }
            // Assert
            Assert.AreEqual(testDesc, _db.Books.First().Description);
        }

        [Test]
        public void SqlBulkTools_WithConflictingTableName_DeletesAndInsertsToCorrectTable()
        {
            // Arrange           
            BulkOperations bulk = new BulkOperations();

            List<SchemaTest2> conflictingSchemaCol = new List<SchemaTest2>();

            for (int i = 0; i < 30; i++)
            {
                conflictingSchemaCol.Add(new SchemaTest2() { ColumnA = "ColumnA " + i });
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<SchemaTest2>()
                        .ForCollection(conflictingSchemaCol)
                        .WithTable("SchemaTest")
                        .WithSchema("AnotherSchema")
                        .AddColumn(x => x.ColumnA)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ColumnA)
                        .Commit(conn); // Remove existing rows

                    bulk.Setup<SchemaTest2>()
                        .ForCollection(conflictingSchemaCol)
                        .WithTable("SchemaTest")
                        .WithSchema("AnotherSchema")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn); // Add new rows

                }

                trans.Complete();
            }

            // Assert
            Assert.IsTrue(_db.SchemaTest2.Any());

        }

        [Test]
        public void SqlBulkTools_BulkDeleteOnId_AddItemsThenRemovesAllItems()
        {
            // Arrange           
            BulkOperations bulk = new BulkOperations();

            List<SchemaTest1> col = new List<SchemaTest1>();

            for (int i = 0; i < 30; i++)
            {
                col.Add(new SchemaTest1() { ColumnB = "ColumnA " + i });
            }

            // Act
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<SchemaTest1>()
                        .ForCollection(col)
                        .WithTable("SchemaTest") // Don't specify schema. Default schema dbo is used. 
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    using (TestContext db = new TestContext(conn, false))
                    {
                        var allItems = db.SchemaTest1.ToList();

                        bulk.Setup<SchemaTest1>()
                            .ForCollection(allItems)
                            .WithTable("SchemaTest")
                            .AddColumn(x => x.Id)
                            .BulkDelete()
                            .MatchTargetOn(x => x.Id)
                            .Commit(conn);
                    }



                }
                trans.Complete();
            }


            // Assert

            Assert.IsFalse(_db.SchemaTest1.Any());
        }

        [Test]
        public void SqlBulkTools_BulkUpdate_PartialUpdateOnlyUpdatesSelectedColumns()
        {
            // Arrange
            BulkOperations bulk = new BulkOperations();
            _bookCollection = _randomizer.GetRandomCollection(30);

            BulkDelete(_db.Books.ToList());
            BulkInsert(_bookCollection);

            // Update just the price on element 5
            int elemToUpdate = 5;
            decimal updatedPrice = 9999999;
            var originalElement = _bookCollection.ElementAt(elemToUpdate);
            _bookCollection.ElementAt(elemToUpdate).Price = updatedPrice;



            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    // Act           
                    bulk.Setup<Book>()
                        .ForCollection(_bookCollection)
                        .WithTable("Books")
                        .AddColumn(x => x.Price)
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .Commit(conn);
                }

                trans.Complete();
            }

            // Assert
            Assert.AreEqual(updatedPrice, _db.Books.Single(x => x.ISBN == originalElement.ISBN).Price);

            /* Profiler shows: MERGE INTO [SqlBulkTools].[dbo].[Books] WITH (HOLDLOCK) AS Target USING #TmpTable 
             * AS Source ON Target.ISBN = Source.ISBN WHEN MATCHED THEN UPDATE SET Target.Price = Source.Price, 
             * Target.ISBN = Source.ISBN ; DROP TABLE #TmpTable; */

        }

        [Test]
        public void SqlBulkTools_BulkInsertWithColumnMappings_CorrectlyMapsColumns()
        {
            BulkOperations bulk = new BulkOperations();

            List<CustomColumnMappingTest> col = new List<CustomColumnMappingTest>();

            for (int i = 0; i < 30; i++)
            {
                col.Add(new CustomColumnMappingTest() { NaturalId = i, ColumnXIsDifferent = "ColumnX " + i, ColumnYIsDifferentInDatabase = i });
            }

            _db.CustomColumnMappingTest.RemoveRange(_db.CustomColumnMappingTest.ToList());
            _db.SaveChanges();

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<CustomColumnMappingTest>()
                        .ForCollection(col)
                        .WithTable("CustomColumnMappingTests")
                        .AddAllColumns()
                        .CustomColumnMapping(x => x.ColumnXIsDifferent, "ColumnX")
                        .CustomColumnMapping(x => x.ColumnYIsDifferentInDatabase, "ColumnY")
                        .BulkInsert()
                        .Commit(conn);
                }

                trans.Complete();
            }

            // Assert
            Assert.IsTrue(_db.CustomColumnMappingTest.Any());
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithColumnMappings_CorrectlyMapsColumns()
        {
            BulkOperations bulk = new BulkOperations();

            List<CustomColumnMappingTest> col = new List<CustomColumnMappingTest>();

            for (int i = 0; i < 30; i++)
            {
                col.Add(new CustomColumnMappingTest() { NaturalId = i, ColumnXIsDifferent = "ColumnX " + i, ColumnYIsDifferentInDatabase = i });
            }

            _db.CustomColumnMappingTest.RemoveRange(_db.CustomColumnMappingTest.ToList());
            _db.SaveChanges();

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<CustomColumnMappingTest>()
                        .ForCollection(col)
                        .WithTable("CustomColumnMappingTests")
                        .AddAllColumns()
                        .CustomColumnMapping(x => x.ColumnXIsDifferent, "ColumnX")
                        .CustomColumnMapping(x => x.ColumnYIsDifferentInDatabase, "ColumnY")
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.NaturalId)
                        .UpdateWhen(x => x.ColumnXIsDifferent != "me")
                        .Commit(conn);
                }

                trans.Complete();
            }

            // Assert
            Assert.IsTrue(_db.CustomColumnMappingTest.Any());
        }

        [Test]
        public void SqlBulkTools_WhenUsingReservedSqlKeywords()
        {
            _db.ReservedColumnNameTest.RemoveRange(_db.ReservedColumnNameTest.ToList());
            BulkOperations bulk = new BulkOperations();

            var list = new List<ReservedColumnNameTest>();

            for (int i = 0; i < 30; i++)
            {
                list.Add(new ReservedColumnNameTest() { Key = i });
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<ReservedColumnNameTest>()
                        .ForCollection(list)
                        .WithTable("ReservedColumnNameTests")
                        .AddAllColumns()
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.Id)
                        .SetIdentityColumn(x => x.Id)
                        .Commit(conn);

                }

                trans.Complete();
            }

            Assert.IsTrue(_db.ReservedColumnNameTest.Any());
        }

        [Test]
        public void SqlBulkTools_BulkInsertOrUpdate_TestIdentityOutput()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType
                        .InputOutput).Commit(conn);
                }

                trans.Complete();
            }

            var test = _db.Books.ToList().ElementAt(10); // Random book within the 30 elements
            var expected = books.Single(x => x.ISBN == test.ISBN);

            Assert.AreEqual(expected.Id, test.Id);

        }

        [Test]
        public void SqlBulkTools_BulkInsertOrUpdate_ExcludeColumnTest()
        {
            // Remove existing records for a fresh test
            BulkDelete(_db.Books.ToList());
            BulkOperations bulk = new BulkOperations();
            // Get a list with random data
            List<Book> books = _randomizer.GetRandomCollection(30);
            DateTime originalDate, updatedDate;

            // Set the original date as the date Donald Trump somehow won the US election. 
            originalDate = new DateTime(2016, 11, 9);
            // Set the new date as the date Trump's presidency will end
            updatedDate = new DateTime(2020, 11, 9);
            
            // Add dates to initial list
            books.ForEach(x =>
            {
                x.CreatedAt = originalDate;
                x.ModifiedAt = originalDate;
            });

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    // Insert initial list
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .SetIdentityColumn(x => x.Id)
                        .Commit(conn);

                    // Update list with new dates
                    books.ForEach(x =>
                    {
                        x.CreatedAt = updatedDate;
                        x.ModifiedAt = updatedDate;
                    });

                    // Insert a random record
                    books.Add(new Book(){CreatedAt   = updatedDate, ModifiedAt = updatedDate, Price = 29.99M, Title="Trump likes woman", ISBN = "1234567891011"});
                    
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns() // Both ModifiedAt and CreatedAt are added implicitly here
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id)
                        .ExcludeColumnFromUpdate(x => x.CreatedAt) // Insert or update with new dates but ignore created date. 
                        .Commit(conn);
                }

                trans.Complete();
            }
            string updatedIsbn = books[10].ISBN;
            string addedIsbn = books.Last().ISBN;
            var updatedBookUnderTest = _db.Books.Single(x => x.ISBN.Equals(updatedIsbn));
            var createdBookUnderTest = _db.Books.Single(x => x.ISBN.Equals(addedIsbn));

            Assert.AreEqual(updatedDate, updatedBookUnderTest.ModifiedAt); // The ModifiedAt should be updated
            Assert.AreEqual(originalDate, updatedBookUnderTest.CreatedAt); // The CreatedAt should be unchanged       
            Assert.AreEqual(updatedDate, createdBookUnderTest.CreatedAt); // CreatedAt should be new date because it was an insert
        }

        [Test]
        public void SqlBulkTools_BulkInsertOrUpdateWithSelectedColumns_TestIdentityOutput()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .AddColumn(x => x.Description)
                        .AddColumn(x => x.Title)
                        .AddColumn(x => x.Price)
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                        .Commit(conn);
                }

                trans.Complete();
            }

            var test = _db.Books.ToList().ElementAt(10); // Random book within the 30 elements
            var expected = books.Single(x => x.ISBN == test.ISBN);

            Assert.AreEqual(expected.Id, test.Id);

        }

        [Test]
        public void SqlBulkTools_BulkInsert_TestIdentityOutput()
        {
            _db.Books.RemoveRange(_db.Books.ToList());

            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            _db.Books.AddRange(_randomizer.GetRandomCollection(60)); // Add some random items before test. 
            _db.SaveChanges();

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                        .Commit(conn);

                }

                trans.Complete();
            }

            var test = _db.Books.ToList().ElementAt(80); // Random between random items before test and total items after test. 
            var expected = books.Single(x => x.ISBN == test.ISBN);

            Assert.AreEqual(expected.Id, test.Id);
        }



        [Test]
        public void SqlBulkTools_BulkInsertWithSelectedColumns_TestIdentityOutput()
        {

            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();

            List<Book> books = _randomizer.GetRandomCollection(30);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    BulkOperations bulk = new BulkOperations();
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .WithBulkCopyBatchSize(5000)
                        .AddColumn(x => x.Title)
                        .AddColumn(x => x.Price)
                        .AddColumn(x => x.Description)
                        .AddColumn(x => x.ISBN)
                        .AddColumn(x => x.PublishDate)
                        .TmpDisableAllNonClusteredIndexes()
                        .BulkInsert()
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                        .Commit(conn);

                }

                trans.Complete();
            }

            var test = _db.Books.ToList().ElementAt(15); // Random book within the 30 elements
            var expected = books.Single(x => x.ISBN == test.ISBN);

            Assert.AreEqual(expected.Id, test.Id);
        }

        [Test]
        public void SqlBulkTools_BulkDeleteWithSelectedColumns_TestIdentityOutput()
        {

            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();

            using (
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlBulkToolsTest"].ConnectionString)
                )
            using (var command = new SqlCommand(
                "DBCC CHECKIDENT ('[dbo].[Books]', RESEED, 10);", conn)
            {
                CommandType = CommandType.Text
            })
            {
                conn.Open();
                command.ExecuteNonQuery();
            }

            List<Book> books = _randomizer.GetRandomCollection(30);
            BulkInsert(books);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    BulkOperations bulk = new BulkOperations();
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .WithBulkCopyBatchSize(5000)
                        .AddColumn(x => x.ISBN)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                        .Commit(conn);
                }

                trans.Complete();
            }

            var test = books.First();
            var expected = 11;

            Assert.AreEqual(expected, test.Id);
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithSelectedColumns_TestIdentityOutput()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);
            BulkInsert(books);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .AddColumn(x => x.Description)
                        .AddColumn(x => x.Title)
                        .AddColumn(x => x.Price)
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                        .Commit(conn);
                }

                trans.Complete();
            }

            var test = _db.Books.ToList().ElementAt(10); // Random book within the 30 elements
            var expected = books.Single(x => x.ISBN == test.ISBN);

            Assert.AreEqual(expected.Id, test.Id);
        }

        [Test]
        public void SqlBulkTools_BulkInsertAddInvalidDataType_ThrowsSqlBulkToolsExceptionException()
        {
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);
            BulkInsert(books);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    Assert.Throws<SqlBulkToolsException>(() => bulk.Setup<Book>()
                                .ForCollection(books)
                                .WithTable("Books")
                                .AddColumn(x => x.ISBN)
                                .AddColumn(x => x.InvalidType)
                                .BulkInsert()
                                .Commit(conn), "Only value, string, char[], byte[] and SqlXml " +
                                               "types can be used with SqlBulkTools. Refer to " +
                                               "https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx for" +
                                               " more details.");
                }

                trans.Complete();
            }


        }

        [Test]
        public void SqlBulkTools_BulkInsertWithGenericType()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            _bookCollection = _randomizer.GetRandomCollection(30);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup()
                    .ForCollection(
                        _bookCollection.Select(
                            x => new { x.Description, x.ISBN, x.Id, x.Price }))
                    .WithTable("Books")
                    .AddColumn(x => x.Id)
                    .AddColumn(x => x.Description)
                    .AddColumn(x => x.ISBN)
                    .AddColumn(x => x.Price)
                    .BulkInsert()
                    .SetIdentityColumn(x => x.Id)
                    .Commit(conn);

                }

                trans.Complete();
            }

            Assert.IsTrue(_db.Books.Any());
        }

        [Test]
        public void SqlBulkTools_BulkInsertWithoutSetter_ThrowsMeaningfulException()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            _bookCollection = _randomizer.GetRandomCollection(30);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    Assert.Throws<SqlBulkToolsException>(() =>
                        bulk.Setup()
                            .ForCollection(
                                _bookCollection.Select(
                                    x => new { x.Description, x.ISBN, x.Id, x.Price }))
                            .WithTable("Books")
                            .AddColumn(x => x.Id)
                            .AddColumn(x => x.Description)
                            .AddColumn(x => x.ISBN)
                            .AddColumn(x => x.Price)
                            .BulkInsert()
                            .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                            .Commit(conn),
                        "No setter method available on property 'Id'. Could not write output back to property.");
                }

                trans.Complete();
            }

        }

        [Test]
        public void SqlBulkTools_BulkInsertOrUpdateWithPrivateIdentityField_ThrowsMeaningfulException()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);
            List<BookWithPrivateIdentity> booksWithPrivateIdentity = new List<BookWithPrivateIdentity>();

            books.ForEach(x => booksWithPrivateIdentity.Add(new BookWithPrivateIdentity()
            {
                ISBN = x.ISBN,
                Description = x.Description,
                Price = x.Price

            }));

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    Assert.Throws<SqlBulkToolsException>(() => bulk.Setup<BookWithPrivateIdentity>()
                            .ForCollection(booksWithPrivateIdentity)
                            .WithTable("Books")
                            .AddColumn(x => x.Id)
                            .AddColumn(x => x.Description)
                            .AddColumn(x => x.ISBN)
                            .AddColumn(x => x.Price)
                            .BulkInsertOrUpdate()
                            .MatchTargetOn(x => x.ISBN)
                            .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                            .Commit(conn),
                        "No setter method available on property 'Id'. Could not write output back to property.");
                }

                trans.Complete();
            }
        }

        [Test]
        public void SqlBulkTools_BulkInsertOrUpdateWithDeletePredicate_OnlyDeletesRecordsFromSpecifiedWarehouse()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                if (i > books.Count / 2 - 1)
                {
                    books[i].WarehouseId = 1;
                }
                else
                {
                    books[i].WarehouseId = 2;
                }
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    books = _randomizer.GetRandomCollection(30);

                    for (int i = 0; i < books.Count; i++)
                    {
                        if (i > books.Count / 2 - 1)
                        {
                            books[i].WarehouseId = 1;
                        }
                        else
                        {
                            books[i].WarehouseId = 2;
                        }
                    }

                    // Only delete if WarehouseId is 1
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .DeleteWhen(x => x.WarehouseId == 1)
                        .SetIdentityColumn(x => x.Id)
                        .Commit(conn);

                }

                trans.Complete();
            }

            // 15 were initially added with warehouse id 2, 15 more were added in second insert. 
            Assert.AreEqual(30, _db.Books.Count(x => x.WarehouseId == 2));

            // 15 were initially added with warehouse id 1. 15 were deleted in second call and 15 were inserted.  
            Assert.AreEqual(15, _db.Books.Count(x => x.WarehouseId == 1));
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithPredicate_OnlyUpdateWhenWarehouseIs1()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                if (i > books.Count / 2 - 1)
                {
                    books[i].WarehouseId = 1;
                }
                else
                {
                    books[i].WarehouseId = 2;
                }
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    for (int i = 0; i < books.Count; i++)
                    {
                        books[i].Price = 99999999;
                    }

                    // Only update if warehouse is 1
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id)
                        .UpdateWhen(x => x.WarehouseId == 1)
                        .Commit(conn);

                }

                trans.Complete();
            }



            Assert.AreEqual(99999999, _db.Books.First(x => x.WarehouseId == 1).Price);
            Assert.AreNotEqual(99999999, _db.Books.First(x => x.WarehouseId == 2).Price);
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithPredicate_WhenBestSellerTrue()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            books[17].BestSeller = true;

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    books[17].Price = 1234567;

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id)
                        .UpdateWhen(x => x.BestSeller == true)
                        .Commit(conn);
                }

                trans.Complete();
            }

            string isbn = books[17].ISBN;

            Assert.AreEqual(1234567, _db.Books.Single(x => x.ISBN.Equals(isbn)).Price);
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithPredicate_WhenBestSellerFalse()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            foreach (var book in books)
                book.BestSeller = true;

            books[17].BestSeller = false;

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    books[17].Price = 1234567;

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .SetIdentityColumn(x => x.Id)
                        .UpdateWhen(x => x.BestSeller == false)
                        .Commit(conn);

                }

                trans.Complete();
            }


            string isbn = books[17].ISBN;

            Assert.AreEqual(1234567, _db.Books.Single(x => x.ISBN.Equals(isbn)).Price);
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithPredicate_OnlyUpdateWhenPriceLessThanOrEqualTo20()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                books[i].Price = 21;
            }

            books[0].Price = 15;

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    books[0].Price = 17;

                    // Only update if price less than or equal to 20
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.Price)
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .UpdateWhen(x => x.Price <= 20).Commit(conn);


                }

                trans.Complete();

            }
            string isbn = books[0].ISBN;

            Assert.AreEqual(1, _db.Books.Count(x => x.Price <= 20));
            Assert.AreEqual(17, _db.Books.Single(x => x.ISBN.Equals(isbn)).Price);
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithPredicate_OnlyDeleteWhenWarehouseIs1()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                if (i > books.Count / 2 - 1)
                {
                    books[i].WarehouseId = 1;
                }
                else
                {
                    books[i].WarehouseId = 2;
                }
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    // Only delete if warehouse is 1
                    bulk.Setup()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ISBN)
                        .DeleteWhen(x => x.WarehouseId == 1)
                        .Commit(conn);
                }

                trans.Complete();
            }



            Assert.IsFalse(_db.Books.Any(x => x.WarehouseId == 1));
        }

        [Test]
        public void SqlBulkTools_BulkDeleteWithMultiplePredicate_WhenDescriptionIsNullAndPriceMoreThan10()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                if (i < 5)
                {
                    books[i].Description = null;
                    books[i].Price = 12;
                }
                else
                    books[i].Price = 30;

            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {

                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    // Only delete when price more than 10 and description is null
                    bulk.Setup()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ISBN)
                        .DeleteWhen(x => x.Price > 10)
                        .DeleteWhen(x => x.Description == null)
                        .Commit(conn);
                }

                trans.Complete();
            }



            Assert.AreEqual(25, _db.Books.Count());
        }

        [Test]
        public void SqlBulkTools_BulkDeleteWithMultiplePredicate_WhenDescriptionIsNotNullAndPriceLessThan10()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                if (i >= 10 && i < 15)
                {
                    books[i].Description = null;
                    books[i].Price = 5;
                }
                else
                    books[i].Price = 9;

            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    // Only delete when price more than 10 and description is null
                    bulk.Setup()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ISBN)
                        .DeleteWhen(x => x.Price < 10)
                        .DeleteWhen(x => x.Description != null)
                        .Commit(conn);
                }

                trans.Complete();
            }

            Assert.AreEqual(5, _db.Books.Count());
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithPredicate_OnlyDeleteWhenPriceMoreThan50()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            for (int i = 0; i < books.Count; i++)
            {
                if (i != 23 && i != 5)
                    books[i].Price = 51;
                else
                {
                    books[i].Price = 32;
                }
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .Commit(conn);

                    // Only delete if price more than 50
                    bulk.Setup()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ISBN)
                        .DeleteWhen(x => x.Price > 50)
                        .Commit(conn); ;
                }

                trans.Complete();
            }

            Assert.AreEqual(2, _db.Books.Count());
        }

        [Test]
        public void SqlBulkTools_BulkUpdateWithNullablePredicate()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();
            BulkOperations bulk = new BulkOperations();

            List<Book> books = _randomizer.GetRandomCollection(30);

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(books)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsert()
                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
                        .Commit(conn);

                }

                trans.Complete();
            }

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    Book book = new Book()
                    {
                        TestNullableInt = 40
                    };

                    bulk.Setup<Book>()
                        .ForObject(new Book() { TestNullableInt = book.TestNullableInt })
                        .WithTable("Books")
                        .AddColumn(x => x.TestNullableInt)
                        .Update()
                        .Where(x => x.Id == 10)
                        .And(x => x.TestNullableInt == book.TestNullableInt)
                        .Commit(conn);
                }

                trans.Complete();
            }
        }

        [Test]
        public void SqlBulkTools_BulkInsertOrUpdae_TestDataTypes()
        {
            _db.Books.RemoveRange(_db.Books.ToList());
            _db.SaveChanges();

            var todaysDate = DateTime.Today;
            Guid guid = Guid.NewGuid();

            BulkOperations bulk = new BulkOperations();
            List<TestDataType> dataTypeTest = new List<TestDataType>()
            {
                new TestDataType()
                {
                    BigIntTest = 342324324324324324,
                    TinyIntTest = 126,
                    DateTimeTest = todaysDate,
                    DateTime2Test = new DateTime(2008, 12, 12, 10, 20, 30),
                    DateTest = new DateTime(2007, 7, 5, 20, 30, 10),
                    TimeTest = new TimeSpan(23, 32, 23),
                    SmallDateTimeTest = new DateTime(2005, 7, 14),
                    BinaryTest = new byte[] {0, 3, 3, 2, 4, 3},
                    VarBinaryTest = new byte[] {3, 23, 33, 243},
                    DecimalTest = 178.43M,
                    MoneyTest = 24333.99M,
                    SmallMoneyTest = 103.32M,
                    RealTest = 32.53F,
                    NumericTest = 154343.3434342M,
                    FloatTest = 232.43F,
                    FloatTest2 = 43243.34,
                    TextTest = "This is some text.",
                    GuidTest = guid,
                    CharTest = new char[] {'S', 'o', 'm', 'e' },
                    XmlTest = "<title>The best SQL Bulk tool</title>",
                    NCharTest = "SomeText",
                    ImageTest = new byte[] {3,3,32,4}
                }
            };

            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<TestDataType>()
                        .ForCollection(dataTypeTest)
                        .WithTable("TestDataTypes")
                        .AddAllColumns()
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.TimeTest)
                        .Commit(conn);
                }

                trans.Complete();
            }


            using (
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlBulkToolsTest"].ConnectionString)
                )
            using (var command = new SqlCommand("SELECT TOP 1 * FROM [dbo].[TestDataTypes]", conn)
            {
                CommandType = CommandType.Text
            })
            {
                conn.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Assert.AreEqual(232.43F, reader["FloatTest"]);
                        Assert.AreEqual(43243.34, reader["FloatTest2"]);
                        Assert.AreEqual(178.43M, reader["DecimalTest"]);
                        Assert.AreEqual(24333.99M, reader["MoneyTest"]);
                        Assert.AreEqual(103.32M, reader["SmallMoneyTest"]);
                        Assert.AreEqual(32.53M, reader["RealTest"]);
                        Assert.AreEqual(154343.3434342M, reader["NumericTest"]);
                        Assert.AreEqual(todaysDate, reader["DateTimeTest"]);
                        Assert.AreEqual(new DateTime(2008, 12, 12, 10, 20, 30), reader["DateTime2Test"]);
                        Assert.AreEqual(new DateTime(2005, 7, 14), reader["SmallDateTimeTest"]);
                        Assert.AreEqual(new DateTime(2007, 7, 5), reader["DateTest"]);
                        Assert.AreEqual(new TimeSpan(23, 32, 23), reader["TimeTest"]);
                        Assert.AreEqual(guid, reader["GuidTest"]);
                        Assert.AreEqual("This is some text.", reader["TextTest"]);
                        Assert.AreEqual(new char[] { 'S', 'o', 'm', 'e' }, reader["CharTest"].ToString().Trim());
                        Assert.AreEqual(126, reader["TinyIntTest"]);
                        Assert.AreEqual(342324324324324324, reader["BigIntTest"]);
                        Assert.AreEqual("<title>The best SQL Bulk tool</title>", reader["XmlTest"]);
                        Assert.AreEqual("SomeText", reader["NCharTest"].ToString().Trim());
                        Assert.AreEqual(new byte[] { 3, 3, 32, 4 }, reader["ImageTest"]);
                        Assert.AreEqual(new byte[] { 0, 3, 3, 2, 4, 3 }, reader["BinaryTest"]);
                        Assert.AreEqual(new byte[] { 3, 23, 33, 243 }, reader["VarBinaryTest"]);
                    }
                }
            }


        }

        private long BulkInsert(IEnumerable<Book> col)
        {
            BulkOperations bulk = new BulkOperations();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(col)
                        .WithTable("Books")
                        .WithBulkCopyBatchSize(5000)
                        .AddColumn(x => x.Title)
                        .AddColumn(x => x.Price)
                        .AddColumn(x => x.Description)
                        .AddColumn(x => x.ISBN)
                        .AddColumn(x => x.PublishDate)
                        .TmpDisableAllNonClusteredIndexes()
                        .BulkInsert()
                        .Commit(conn);
                }

                trans.Complete();
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return elapsedMs;
        }

        private long BulkInsertAllColumns(IEnumerable<Book> col)
        {
            BulkOperations bulk = new BulkOperations();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(col)
                        .WithTable("Books")
                        .AddAllColumns()
                        .TmpDisableAllNonClusteredIndexes()
                        .BulkInsert()
                        .Commit(conn);
                }

                trans.Complete();
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return elapsedMs;
        }

        private long BulkInsertOrUpdate(IEnumerable<Book> col)
        {
            BulkOperations bulk = new BulkOperations();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(col)
                        .WithTable("Books")
                        .AddColumn(x => x.Title)
                        .AddColumn(x => x.Price)
                        .AddColumn(x => x.Description)
                        .AddColumn(x => x.ISBN)
                        .AddColumn(x => x.PublishDate)
                        .BulkInsertOrUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .Commit(conn);
                }

                trans.Complete();
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return elapsedMs;
        }

        private long BulkInsertOrUpdateAllColumns(IEnumerable<Book> col)
        {
            BulkOperations bulk = new BulkOperations();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(col)
                        .WithTable("Books")
                        .AddAllColumns()
                        .BulkInsertOrUpdate()
                        .SetIdentityColumn(x => x.Id)
                        .MatchTargetOn(x => x.ISBN)
                        .Commit(conn);
                }

                trans.Complete();
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return elapsedMs;
        }

        private long BulkUpdate(IEnumerable<Book> col)
        {
            BulkOperations bulk = new BulkOperations();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(col)
                        .WithTable("Books")
                        .AddColumn(x => x.Title)
                        .AddColumn(x => x.Price)
                        .AddColumn(x => x.Description)
                        .AddColumn(x => x.PublishDate)
                        .BulkUpdate()
                        .MatchTargetOn(x => x.ISBN)
                        .Commit(conn);
                }

                trans.Complete();
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return elapsedMs;
        }

        private long BulkDelete(IEnumerable<Book> col)
        {
            BulkOperations bulk = new BulkOperations();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (TransactionScope trans = new TransactionScope())
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager
                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
                {
                    bulk.Setup<Book>()
                        .ForCollection(col)
                        .WithTable("Books")
                        .AddColumn(x => x.ISBN)
                        .BulkDelete()
                        .MatchTargetOn(x => x.ISBN)
                        .Commit(conn);
                }

                trans.Complete();
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return elapsedMs;
        }
    }
}