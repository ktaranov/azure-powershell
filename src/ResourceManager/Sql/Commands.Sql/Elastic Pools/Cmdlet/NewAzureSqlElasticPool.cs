// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.ResourceManager.Common.Tags;
using Microsoft.Azure.Commands.Sql.Database.Model;
using Microsoft.Azure.Commands.Sql.ElasticPool.Model;
using Microsoft.Rest.Azure;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Azure.Commands.Sql.ElasticPool.Cmdlet
{
    /// <summary>
    /// Cmdlet to create a new Azure Sql ElasticPool
    /// </summary>
    [Cmdlet(VerbsCommon.New, "AzureRmSqlElasticPool", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Low, DefaultParameterSetName = "DtuBasedPool")]
    public class NewAzureSqlElasticPool : AzureSqlElasticPoolCmdletBase
    {
        /// <summary>
        /// Gets or sets the name of the Elastic Pool to create.
        /// </summary>
        [Parameter(Mandatory = true,
            Position = 2,
            HelpMessage = "The name of the Azure SQL ElasticPool to create.")]
        [Alias("Name")]
        [ValidateNotNullOrEmpty]
        public string ElasticPoolName { get; set; }

        /// <summary>
        /// Gets or sets the edition to assign to the Azure SQL Elastic Pool
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "The edition to assign to the Azure SQL Database.")]
        [ValidateNotNullOrEmpty]
        public DatabaseEdition Edition { get; set; }

        /// <summary>
        /// Gets or sets the total shared DTU for the Sql Azure Elastic Pool.
        /// </summary>
        [Parameter(ParameterSetName = DtuPoolParameterSet, Mandatory = false,
            HelpMessage = "The total shared DTU for the Sql Azure Elastic Pool.")]
        [ValidateNotNullOrEmpty]
        public int Dtu { get; set; }

        /// <summary>
        /// Gets or sets the storage limit for the Sql Azure Elastic Pool in MB.
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "The storage limit for the Sql Azure Elastic Pool in MB.")]
        [ValidateNotNullOrEmpty]
        public long StorageMB { get; set; }

        /// <summary>
        /// Gets or sets the minimum DTU all Sql Azure Databases are guaranteed.
        /// </summary>
        [Parameter(ParameterSetName = DtuPoolParameterSet, Mandatory = false,
            HelpMessage = "The minimum DTU all Sql Azure Databases are guaranteed.")]
        [ValidateNotNullOrEmpty]
        public int DatabaseDtuMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum DTU any one Sql Azure Database can consume.
        /// </summary>
        [Parameter(ParameterSetName = DtuPoolParameterSet, Mandatory = false,
            HelpMessage = "The maximum DTU any one Sql Azure Database can consume.")]
        [ValidateNotNullOrEmpty]
        public int DatabaseDtuMax { get; set; }

        /// <summary>
        /// Gets or sets the total shared VCore number for the Sql Azure Elastic Pool.
        /// </summary>
        [Parameter(ParameterSetName = VcorePoolParameterSet, Mandatory = true,
            HelpMessage = "The total shared number of Vcores for the Sql Azure Elastic Pool.")]
        [ValidateNotNullOrEmpty]
        public int Vcore { get; set; }

        /// <summary>
        /// Gets or sets the compute generation for the Sql Azure Elastic Pool
        ///   (Available ComputeGeneration in the format of: GP_Gen4, GP_Gen2, BC_Gen4).
        /// </summary>
        [Parameter(ParameterSetName = VcorePoolParameterSet, Mandatory = true,
            HelpMessage = "The compute generation for the Sql Azure Elastic Pool. e.g. 'GP_Gen4', 'BC_Gen4'.")]
        [ValidateNotNullOrEmpty]
        public string RequestedSkuName { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the Azure Sql Elastic Pool
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "The tags to associate with the Azure Sql Elastic Pool")]
        [Alias("Tag")]
        public Hashtable Tags { get; set; }

        /// <summary>
        /// Gets or sets the zone redundant option to assign to the Azure SQL Elastic Pool
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "The zone redundancy to associate with the Azure Sql Elastic Pool")]
        public SwitchParameter ZoneRedundant { get; set; }

        /// <summary>
        /// Gets or sets whether or not to run this cmdlet in the background as a job
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Run cmdlet in the background")]
        public SwitchParameter AsJob { get; set; }

        /// <summary>
        /// Overriding to add warning message
        /// </summary>
        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();
        }

        /// <summary>
        /// Get the entities from the service
        /// </summary>
        /// <returns>The list of entities</returns>
        protected override IEnumerable<AzureSqlElasticPoolModel> GetEntity()
        {
            // We try to get the database.  Since this is a create, we don't want the database to exist
            try
            {
                ModelAdapter.GetElasticPool(this.ResourceGroupName, this.ServerName, this.ElasticPoolName);
            }
            catch (CloudException ex)
            {
                if (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // This is what we want.  We looked and there is no database with this name.
                    return null;
                }

                // Unexpected exception encountered
                throw;
            }

            // The database already exists
            throw new PSArgumentException(
                string.Format(Microsoft.Azure.Commands.Sql.Properties.Resources.ElasticPoolNameExists, this.ElasticPoolName, this.ServerName),
                "ElasticPoolName");
        }

        /// <summary>
        /// Create the model from user input
        /// </summary>
        /// <param name="model">Model retrieved from service</param>
        /// <returns>The model that was passed in</returns>
        protected override IEnumerable<AzureSqlElasticPoolModel> ApplyUserInputToModel(IEnumerable<AzureSqlElasticPoolModel> model)
        {
            string location = ModelAdapter.GetServerLocation(ResourceGroupName, ServerName);
            List<AzureSqlElasticPoolModel> newEntity = new List<AzureSqlElasticPoolModel>();
            AzureSqlElasticPoolModel newModel = new AzureSqlElasticPoolModel()
            {
                ResourceGroupName = ResourceGroupName,
                ServerName = ServerName,
                ElasticPoolName = ElasticPoolName,
                Tags = TagsConversionHelper.CreateTagDictionary(Tags, validate: true),
                Location = location,
                ZoneRedundant = MyInvocation.BoundParameters.ContainsKey("ZoneRedundant") ? (bool?)ZoneRedundant.ToBool() : null,
                MaxSizeBytes = MyInvocation.BoundParameters.ContainsKey("StorageMB") ? (long?)(StorageMB * Megabytes) : null
            };

            DatabaseEdition? edition = MyInvocation.BoundParameters.ContainsKey("Edition") ? (DatabaseEdition?)Edition : null;
            if (ParameterSetName == DtuPoolParameterSet)
            {            
                if(edition.HasValue)
                {
                    newModel.Sku = new Management.Sql.Models.Sku()
                    {
                        Name = string.Format("{0}{1}", edition.Value, NormalElasticPoolSkuNamesPostfix),
                        Tier = edition.ToString(),
                        Capacity = MyInvocation.BoundParameters.ContainsKey("Dtu") ? (int?)Dtu : null
                    };
                }

                newModel.PerDatabaseSettings = new Management.Sql.Models.ElasticPoolPerDatabaseSettings()
                {
                    MinCapacity = MyInvocation.BoundParameters.ContainsKey("DatabaseDtuMin") ? (double?)DatabaseDtuMin : null,
                    MaxCapacity = MyInvocation.BoundParameters.ContainsKey("DatabaseDtuMax") ? (double?)DatabaseDtuMax : null
                };               
            }
            else
            {
                string skuName = string.Format("{0}_{1}", RequestedSkuName, Vcore);

                newModel.Sku = new Management.Sql.Models.Sku()
                {
                    Name = RequestedSkuName,
                    Tier = edition.HasValue ? edition.ToString() : null,
                    Capacity = Vcore
                };

                newModel.PerDatabaseSettings = new Management.Sql.Models.ElasticPoolPerDatabaseSettings();
            }

            newEntity.Add(newModel);
            return newEntity;
        }

        /// <summary>
        /// Create the new elastic pool
        /// </summary>
        /// <param name="entity">The output of apply user input to model</param>
        /// <returns>The input entity</returns>
        protected override IEnumerable<AzureSqlElasticPoolModel> PersistChanges(IEnumerable<AzureSqlElasticPoolModel> entity)
        {
            return new List<AzureSqlElasticPoolModel>() {
                ModelAdapter.UpsertElasticPool(entity.First())
            };
        }
    }
}
