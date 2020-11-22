using BlendMonitor.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using BlendMonitor.Repository;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading.Tasks;
using static BlendMonitor.Constans;
using BlendMonitor.Service;
using BlendMonitor.Model;

namespace BlendMonitor.UnitTest
{
    public class BlendMonitorTesting
    {

        public BlendMonitorContext dbContext;
        public CustomRepo repo;
        IConfiguration configuration;
        BlendMonitorService blendMonitorService;

        [OneTimeSetUp]
        public void AssemblyInit()
        {
            var options = new DbContextOptionsBuilder<BlendMonitorContext>()
                                  .UseInMemoryDatabase(Guid.NewGuid().ToString())
                                  .Options;
            dbContext = new BlendMonitorContext(options);           
        }

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            // Executes once for the test class. (Optional)   E:\TankUT\MockJson\MockData.json
            string tableData = "";
            var obj = JObject.Parse("{ }");
            dynamic jsonModel;

            var jsonString = File.ReadAllText(TestContext.CurrentContext.TestDirectory + "\\BlendMonitorMock.json");
            obj = JObject.Parse(jsonString);

            #region table insert
            tableData = obj["AbcPrograms"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrograms>>(tableData);
            //var jsonModel = JsonConvert.DeserializeObject<List<AbcPrograms>>("[\r\n  {\r\n    \"NAME\": \"ABC ANALYZER MONITOR\"\r\n      }\r\n]");
            dbContext.AbcPrograms.AddRange(jsonModel);

            tableData = obj["AbcAnzHdrProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcAnzHdrProps>>(tableData);
            dbContext.AbcAnzHdrProps.AddRange(jsonModel);

            tableData = obj["AbcAnzs"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcAnzs>>(tableData);
            dbContext.AbcAnzs.AddRange(jsonModel);

            tableData = obj["AbcAnzsStates"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcAnzsStates>>(tableData);
            dbContext.AbcAnzsStates.AddRange(jsonModel);

            tableData = obj["AbcBlendCompProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendCompProps>>(tableData);
            dbContext.AbcBlendCompProps.AddRange(jsonModel);

            tableData = obj["AbcBlendComps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendComps>>(tableData);
            dbContext.AbcBlendComps.AddRange(jsonModel);

            tableData = obj["AbcBlendDest"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendDest>>(tableData);
            dbContext.AbcBlendDest.AddRange(jsonModel);

            tableData = obj["AbcBlendDestProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendDestProps>>(tableData);
            dbContext.AbcBlendDestProps.AddRange(jsonModel);

            tableData = obj["AbcBlendDestSeq"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendDestSeq>>(tableData);
            dbContext.AbcBlendDestSeq.AddRange(jsonModel);

            tableData = obj["AbcBlendIntervalComps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendIntervalComps>>(tableData);
            dbContext.AbcBlendIntervalComps.AddRange(jsonModel);

            tableData = obj["AbcBlendIntervalProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendIntervalProps>>(tableData);
            dbContext.AbcBlendIntervalProps.AddRange(jsonModel);

            tableData = obj["AbcBlendIntervals"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendIntervals>>(tableData);
            dbContext.AbcBlendIntervals.AddRange(jsonModel);

            tableData = obj["AbcBlendProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendProps>>(tableData);
            dbContext.AbcBlendProps.AddRange(jsonModel);

            tableData = obj["AbcBlendSampleProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendSampleProps>>(tableData);
            dbContext.AbcBlendSampleProps.AddRange(jsonModel);

            tableData = obj["AbcBlendSamples"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendSamples>>(tableData);
            dbContext.AbcBlendSamples.AddRange(jsonModel);

            tableData = obj["AbcBlendSourceSeq"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendSourceSeq>>(tableData);
            dbContext.AbcBlendSourceSeq.AddRange(jsonModel);

            tableData = obj["AbcBlendSources"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendSources>>(tableData);
            dbContext.AbcBlendSources.AddRange(jsonModel);

            tableData = obj["AbcBlendStations"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendStations>>(tableData);
            dbContext.AbcBlendStations.AddRange(jsonModel);

            tableData = obj["AbcBlendSwings"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlendSwings>>(tableData);
            dbContext.AbcBlendSwings.AddRange(jsonModel);

            tableData = obj["AbcBlenderComps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlenderComps>>(tableData);
            dbContext.AbcBlenderComps.AddRange(jsonModel);

            tableData = obj["AbcBlenderDest"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlenderDest>>(tableData);
            dbContext.AbcBlenderDest.AddRange(jsonModel);

            tableData = obj["AbcBlenderSources"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlenderSources>>(tableData);
            dbContext.AbcBlenderSources.AddRange(jsonModel);

            tableData = obj["AbcBlenders"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlenders>>(tableData);
            dbContext.AbcBlenders.AddRange(jsonModel);

            tableData = obj["AbcBlends"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcBlends>>(tableData);
            dbContext.AbcBlends.AddRange(jsonModel);

            tableData = obj["AbcCalcCoefficients"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcCalcCoefficients>>(tableData);
            dbContext.AbcCalcCoefficients.AddRange(jsonModel);

            tableData = obj["AbcCalcRoutines"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcCalcRoutines>>(tableData);
            dbContext.AbcCalcRoutines.AddRange(jsonModel);

            tableData = obj["AbcCompLineupEqp"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcCompLineupEqp>>(tableData);
            dbContext.AbcCompLineupEqp.AddRange(jsonModel);

            tableData = obj["AbcCompLineups"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcCompLineups>>(tableData);
            dbContext.AbcCompLineups.AddRange(jsonModel);

            tableData = obj["AbcGrades"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcGrades>>(tableData);
            dbContext.AbcGrades.AddRange(jsonModel);

            tableData = obj["AbcIcons"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcIcons>>(tableData);
            dbContext.AbcIcons.AddRange(jsonModel);

            tableData = obj["AbcLabTankData"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcLabTankData>>(tableData);
            dbContext.AbcLabTankData.AddRange(jsonModel);

            tableData = obj["AbcLineupGeo"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcLineupGeo>>(tableData);
            dbContext.AbcLineupGeo.AddRange(jsonModel);

            tableData = obj["AbcMaterials"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcMaterials>>(tableData);
            dbContext.AbcMaterials.AddRange(jsonModel);

            tableData = obj["AbcPrdAdditives"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrdAdditives>>(tableData);
            dbContext.AbcPrdAdditives.AddRange(jsonModel);

            tableData = obj["AbcPrdPropSpecs"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrdPropSpecs>>(tableData);
            dbContext.AbcPrdPropSpecs.AddRange(jsonModel);

            tableData = obj["AbcPrdgrpMatProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrdgrpMatProps>>(tableData);
            dbContext.AbcPrdgrpMatProps.AddRange(jsonModel);

            tableData = obj["AbcPrdgrpProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrdgrpProps>>(tableData);
            dbContext.AbcPrdgrpProps.AddRange(jsonModel);

            tableData = obj["AbcPrdgrpUsages"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrdgrpUsages>>(tableData);
            dbContext.AbcPrdgrpUsages.AddRange(jsonModel);

            tableData = obj["AbcPrdgrps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPrdgrps>>(tableData);
            dbContext.AbcPrdgrps.AddRange(jsonModel);

            tableData = obj["AbcProdLineupEqp"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcProdLineupEqp>>(tableData);
            dbContext.AbcProdLineupEqp.AddRange(jsonModel);

            tableData = obj["AbcProdLineups"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcProdLineups>>(tableData);
            dbContext.AbcProdLineups.AddRange(jsonModel);          

            tableData = obj["AbcProjDefaults"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcProjDefaults>>(tableData);
            dbContext.AbcProjDefaults.AddRange(jsonModel);

            tableData = obj["AbcPropSources"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPropSources>>(tableData);
            dbContext.AbcPropSources.AddRange(jsonModel);

            tableData = obj["AbcProperties"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcProperties>>(tableData);
            dbContext.AbcProperties.AddRange(jsonModel);

            tableData = obj["AbcPumps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcPumps>>(tableData);
            dbContext.AbcPumps.AddRange(jsonModel);

            tableData = obj["AbcRbcStates"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcRbcStates>>(tableData);
            dbContext.AbcRbcStates.AddRange(jsonModel);

            tableData = obj["AbcScanGroups"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcScanGroups>>(tableData);
            dbContext.AbcScanGroups.AddRange(jsonModel);

            tableData = obj["AbcStations"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcStations>>(tableData);
            dbContext.AbcStations.AddRange(jsonModel);

            tableData = obj["AbcSwingCriteria"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcSwingCriteria>>(tableData);
            dbContext.AbcSwingCriteria.AddRange(jsonModel);

            tableData = obj["AbcSwingStates"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcSwingStates>>(tableData);
            dbContext.AbcSwingStates.AddRange(jsonModel);

            tableData = obj["AbcTags"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcTags>>(tableData);
            dbContext.AbcTags.AddRange(jsonModel);

            tableData = obj["AbcTankComposition"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcTankComposition>>(tableData);
            dbContext.AbcTankComposition.AddRange(jsonModel);

            tableData = obj["AbcTankProps"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcTankProps>>(tableData);
            dbContext.AbcTankProps.AddRange(jsonModel);

            tableData = obj["AbcTankStates"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcTankStates>>(tableData);
            dbContext.AbcTankStates.AddRange(jsonModel);

            tableData = obj["AbcTanks"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcTanks>>(tableData);
            dbContext.AbcTanks.AddRange(jsonModel);

            tableData = obj["AbcTranstxt"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcTranstxt>>(tableData);
            dbContext.AbcTranstxt.AddRange(jsonModel);

            tableData = obj["AbcUnitConversion"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcUnitConversion>>(tableData);
            dbContext.AbcUnitConversion.AddRange(jsonModel);

            tableData = obj["AbcUom"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcUom>>(tableData);
            dbContext.AbcUom.AddRange(jsonModel);

            tableData = obj["AbcUsages"].ToString().Replace("_", "");
            jsonModel = JsonConvert.DeserializeObject<List<AbcUsages>>(tableData);
            dbContext.AbcUsages.AddRange(jsonModel);

            dbContext.SaveChangesAsync();
            #endregion

            var myConfiguration = new Dictionary<string, string>
                                    {
                                        {"ProgramName", "ABC BLEND MONITOR"},

                                    };

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
            repo = new CustomRepo(dbContext, configuration);
            var mockRepo = new Mock<IBlendMonitorRepository>();
            blendMonitorService = new BlendMonitorService(repo, configuration, new Shared(repo, configuration));


        }

        [TestCase("MEDIUM", 1)]
        [TestCase("HIGH", 2)]
        [TestCase("LOW", 0)]
        public void TestHelperMethod_gArDebugLevelStrs(string expected, int input)
        {
            string result = HelperMethods.gArDebugLevelStrs(input);
            Assert.AreEqual(result, expected);
        }

        [TestCase(-12.06539, 2)]
        [TestCase(24.493160000000007, 15)]
        [TestCase(58.607952, 30)]
        [TestCase(103.13810500000001, 50)]
        public void TestHelperMethod_SSF2CST(double expected, double input)
        {
            double result = HelperMethods.SSF2CST(input);
            Assert.AreEqual(result, expected);
        }

        [TestCase(-128.67, 50)]
        public void TestHelperMethod_SG2API(double expected, double input)
        {
            double result = HelperMethods.SG2API(input);
            Assert.AreEqual(result, expected);
        }

        [TestCase(0.7796143250688705, 50)]
        public void TestHelperMethod_API2SG(double expected, double input)
        {
            double result = HelperMethods.API2SG(input);
            Assert.AreEqual(result, expected);
        }

        [TestCase(122, 50)]
        public void TestHelperMethod_DEGC2DEGF(double expected, double input)
        {
            double result = HelperMethods.DEGC2DEGF(input);
            Assert.AreEqual(result, expected);
        }

        [TestCase(10, 50)]
        public void TestHelperMethod_DEGF2DEGC(double expected, double input)
        {
            double result = HelperMethods.DEGF2DEGC(input);
            Assert.AreEqual(result, expected);
        }

        [Test]
        public void RepoGetCycleTime()
        {
            var data = Task.Run(async () => await blendMonitorService.ProcessBlenders()).GetAwaiter().GetResult();
            Mock.Get(blendMonitorService).Verify(x => x.SetBlendState(It.IsAny<int>(), It.IsAny<List<AbcBlenders>>(), It.IsAny<CurBlendData>(), It.IsAny<DebugLevels>()), Times.Once);
        }

        
    }
}
