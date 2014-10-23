// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Excavator.F1
{
    /// <summary>
    /// Partial of F1Component that holds the Notes import
    /// </summary>
    partial class F1Component
    {
        /// <summary>
        /// Maps the Giftedness Program.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        private void MapGiftednessProgram( IQueryable<Row> tableData )
        {

            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying Giftedness Program import ({0:N0} found).", totalRows ) );

            foreach ( var row in tableData )
            {
                var rockContext = new RockContext();
                var categoryList = new CategoryService( rockContext ).Queryable().ToList();
                var attributeList = new AttributeService( rockContext ).Queryable().ToList();
                var definedTypeList = new DefinedTypeService( rockContext ).Queryable().ToList();
                var definedValueList = new DefinedValueService( rockContext ).Queryable().ToList();

                //check if category exists
                string category = row["CategoryName"] as string;
                if ( categoryList.Find( c => c.Name == category ) == null )
                {
                    var entityType = new EntityTypeService( rockContext );
                    //creates if category doesn't exist
                    var newCategory = new Category();
                    newCategory.IsSystem = false;
                    newCategory.EntityTypeId = entityType.Queryable().Where( e => e.Name == "Rock.Model.Attribute" ).Select( e => e.Id ).FirstOrDefault();
                    newCategory.EntityTypeQualifierColumn = "EntityTypeId";
                    newCategory.EntityTypeQualifierValue = Convert.ToString( PersonEntityTypeId );  //Convert.ToString(entityType.Queryable().Where( e => e.Name == "Rock.Model.Person" ).Select( e => e.Id ).FirstOrDefault());
                    newCategory.Name = category;
                    newCategory.Description = "Contains the spiritual gifts attributes";

                    //var newCategoryContext = new RockContext();
                    //newCategoryContext.WrapTransaction( () =>
                    //{
                    //    newCategoryContext.Configuration.AutoDetectChangesEnabled = false;
                    //    newCategoryContext.Categories.Add( newCategory );
                    //    newCategoryContext.SaveChanges( DisableAudit );
                    //} );
                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.Configuration.AutoDetectChangesEnabled = false;
                        rockContext.Categories.Add( newCategory );
                        rockContext.SaveChanges( DisableAudit );
                    } );
                }
                //Check if Attribute exists
                if ( attributeList.Find( a => a.Key == "Rank1" ) == null || attributeList.Find( a => a.Key == "Rank2" ) == null || attributeList.Find( a => a.Key == "Rank3" ) == null || attributeList.Find( a => a.Key == "Rank4" ) == null )
                {
                    var fieldType = new FieldTypeService( rockContext );
                    var newAttributeList = new List<Rock.Model.Attribute>();
                    var fieldTypeId = fieldType.Queryable().Where( e => e.Name == "Defined Value" ).FirstOrDefault().Id;
                    var category2 = new CategoryService( rockContext ).Queryable().Where( gt => gt.Name == "Spiritual Gifts" ).FirstOrDefault();


                    if ( attributeList.Find( a => a.Key == "Rank1" ) == null )
                    {
                        //Creates if attribute doesn't exist
                        var newAttribute = new Rock.Model.Attribute();
                        newAttribute.Key = "Rank1";
                        newAttribute.Name = "Rank 1";
                        newAttribute.FieldTypeId = fieldTypeId;
                        newAttribute.EntityTypeId = PersonEntityTypeId;
                        newAttribute.EntityTypeQualifierValue = string.Empty;
                        newAttribute.EntityTypeQualifierColumn = string.Empty;
                        newAttribute.Description = "Rank 1";
                        newAttribute.DefaultValue = string.Empty;
                        newAttribute.IsMultiValue = false;
                        newAttribute.IsRequired = false;
                        newAttribute.Categories = new List<Category>();
                        newAttribute.Categories.Add( category2 );

                        newAttributeList.Add( newAttribute );

                    }
                    if ( attributeList.Find( a => a.Key == "Rank2" ) == null )
                    {
                        //Creates if attribute doesn't exist
                        var newAttribute = new Rock.Model.Attribute();
                        newAttribute.Key = "Rank2";
                        newAttribute.Name = "Rank 2";
                        newAttribute.FieldTypeId = fieldTypeId;
                        newAttribute.EntityTypeId = PersonEntityTypeId;
                        newAttribute.EntityTypeQualifierValue = string.Empty;
                        newAttribute.EntityTypeQualifierColumn = string.Empty;
                        newAttribute.Description = "Rank 2";
                        newAttribute.DefaultValue = string.Empty;
                        newAttribute.IsMultiValue = false;
                        newAttribute.IsRequired = false;
                        newAttribute.Categories = new List<Category>();
                        newAttribute.Categories.Add( category2 );

                        newAttributeList.Add( newAttribute );
                    }
                    if ( attributeList.Find( a => a.Key == "Rank3" ) == null )
                    {
                        //Creates if attribute doesn't exist
                        var newAttribute = new Rock.Model.Attribute();
                        newAttribute.Key = "Rank3";
                        newAttribute.Name = "Rank 3";
                        newAttribute.FieldTypeId = fieldTypeId;
                        newAttribute.EntityTypeId = PersonEntityTypeId;
                        newAttribute.EntityTypeQualifierValue = string.Empty;
                        newAttribute.EntityTypeQualifierColumn = string.Empty;
                        newAttribute.Description = "Rank 3";
                        newAttribute.DefaultValue = string.Empty;
                        newAttribute.IsMultiValue = false;
                        newAttribute.IsRequired = false;
                        newAttribute.Categories = new List<Category>();
                        newAttribute.Categories.Add( category2 );

                        newAttributeList.Add( newAttribute );
                    }
                    if ( attributeList.Find( a => a.Key == "Rank4" ) == null )
                    {


                        //Creates if attribute doesn't exist
                        var newAttribute = new Rock.Model.Attribute();
                        newAttribute.Key = "Rank4";
                        newAttribute.Name = "Rank 4";
                        newAttribute.FieldTypeId = fieldTypeId;
                        newAttribute.EntityTypeId = PersonEntityTypeId;
                        newAttribute.EntityTypeQualifierValue = string.Empty;
                        newAttribute.EntityTypeQualifierColumn = string.Empty;
                        newAttribute.Description = "Rank 4";
                        newAttribute.DefaultValue = string.Empty;
                        newAttribute.IsMultiValue = false;
                        newAttribute.IsRequired = false;
                        newAttribute.Categories = new List<Category>();
                        newAttribute.Categories.Add( category2 );


                        newAttributeList.Add( newAttribute );
                    }

                    if ( newAttributeList.Any() )
                    {
                        //var newAttributeContext = new RockContext();
                        rockContext.WrapTransaction( () =>
                        {
                            rockContext.Configuration.AutoDetectChangesEnabled = false;
                            rockContext.Attributes.AddRange( newAttributeList );
                            rockContext.SaveChanges( DisableAudit );
                            newAttributeList.Clear();
                        } );
                    }
                }
                //checks if Defined Type exists
                if ( definedTypeList.Find( d => d.Name == "Spiritual Gifts" ) == null )
                {
                    var fieldTypeService = new FieldTypeService( rockContext );

                    //creates Defined Type
                    var newDefinedType = new DefinedType();
                    newDefinedType.IsSystem = false;
                    newDefinedType.FieldTypeId = fieldTypeService.Queryable().Where( f => f.Name == "Text" ).Select( f => f.Id ).FirstOrDefault();
                    newDefinedType.Name = "Spiritual Gifts";
                    newDefinedType.Description = "Defined Type for Spiritual Gifts values";
                    newDefinedType.CategoryId = categoryList.Find( c => c.Name == "Person" ).Id;

                    //var newDTContext = new RockContext();
                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.Configuration.AutoDetectChangesEnabled = false;
                        rockContext.DefinedTypes.Add( newDefinedType );
                        rockContext.SaveChanges( DisableAudit );
                    } );

                }
                //checks if Defined Value exists
                string attributeName = row["AttributeName"] as string;
                int? giftAttributeId = row["GiftAttributeID"] as int?;
                if ( definedValueList.Find( d => d.Value == attributeName ) == null )
                {
                    var definedTypeService = new DefinedTypeService( rockContext );
                    //creates Defined Value
                    var newDefinedValue = new DefinedValue();
                    newDefinedValue.IsSystem = false;
                    newDefinedValue.DefinedTypeId = definedTypeService.Queryable().Where( d => d.Name == "Spiritual Gifts" ).Select( d => d.Id ).FirstOrDefault();
                    newDefinedValue.Value = attributeName;
                    newDefinedValue.Description = "Spiritual Gift attribute value: " + attributeName;
                    newDefinedValue.ForeignId = Convert.ToString(giftAttributeId);

                    //var newDVContext = new RockContext();
                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.Configuration.AutoDetectChangesEnabled = false;
                        rockContext.DefinedValues.Add( newDefinedValue );
                        rockContext.SaveChanges( DisableAudit );
                    } );

                }

                completed++;

                if ( completed % percentage < 1 )
                {
                    int percentComplete = completed / percentage;
                    ReportProgress( percentComplete, string.Format( "{0:N0} spiritual gifts attributes imported ({1}% complete).", completed, percentComplete ) );
                }
                else if ( completed % ReportingNumber < 1 )
                {

                    ReportPartialProgress();
                }
            }

            ReportProgress( 100, string.Format( "Finished note import: {0:N0} spiritual gifts attributes imported.", completed ) );
        }
        /// <summary>
        /// Maps the Individual Giftedness.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        private void MapIndividualGiftedness( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            var attributeService = new AttributeService( lookupContext );

            int rank1Id = attributeService.Queryable().Where( a => a.Key == "Rank1" ).FirstOrDefault().Id;
            int rank2Id = attributeService.Queryable().Where( a => a.Key == "Rank2" ).FirstOrDefault().Id;
            int rank3Id = attributeService.Queryable().Where( a => a.Key == "Rank3" ).FirstOrDefault().Id;
            int rank4Id = attributeService.Queryable().Where( a => a.Key == "Rank4" ).FirstOrDefault().Id;



            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying Giftedness Program import ({0:N0} found).", totalRows ) );

            var newAttributeValueList = new List<AttributeValue>();

            foreach ( var row in tableData )
            {
                int? individualId = row["Individual_ID"] as int?;
                int personId = (int)GetPersonAliasId( individualId );
                var newAttributeValue = new AttributeValue();
                int? rank = row["Rank"] as int?;
                int rankId = 0;

                //not everyone has all 4 ranks, some are missing the fourth and that was causing it to run in the below if condition and try to create a duplicate record.
                if ( rank == 1 ) { rankId = rank1Id; }
                if ( rank == 2 ) { rankId = rank2Id; }
                if ( rank == 3 ) { rankId = rank3Id; }
                if ( rank == 4 ) { rankId = rank4Id; }


                if ( personId != 0 && rankId != 0 )
                {
                    var attributeValueService = new AttributeValueService( lookupContext );

                    //checks if they are in the database already or if there is a record currently in the newAttributeValueList
                    if ( attributeValueService.Queryable().Where( a => a.AttributeId == rankId && a.EntityId == personId ).FirstOrDefault() == null && newAttributeValueList.Find(a => a.AttributeId == rankId && a.EntityId == personId) == null )
                    {

                        DateTime? assessmentDate = row["AssessmentDate"] as DateTime?;
                        int? giftAttributeId = row["GiftAttributeID"] as int?;
                        string giftAttributeIdString = Convert.ToString( giftAttributeId );
                        


                        var definedValueService = new DefinedValueService( lookupContext );


                        newAttributeValue.IsSystem = false;
                        newAttributeValue.EntityId = personId;

                        if ( rank == 1 ) { newAttributeValue.AttributeId = rank1Id; }
                        if ( rank == 2 ) { newAttributeValue.AttributeId = rank2Id; }
                        if ( rank == 3 ) { newAttributeValue.AttributeId = rank3Id; }
                        if ( rank == 4 ) { newAttributeValue.AttributeId = rank4Id; }

                        newAttributeValue.Value = Convert.ToString( definedValueService.Queryable().Where( a => a.ForeignId == giftAttributeIdString ).FirstOrDefault().Guid );
                        newAttributeValue.CreatedDateTime = assessmentDate;

                        newAttributeValueList.Add( newAttributeValue );
                        completed++;
                    }
                }
                if ( newAttributeValueList.Any() )
                {
                    if ( completed % percentage < 1 )
                    {
                        int percentComplete = completed / percentage;
                        ReportProgress( percentComplete, string.Format( "{0:N0} spiritual gifts imported ({1}% complete).", completed, percentComplete ) );
                    }
                    else if ( completed % ReportingNumber < 1 )
                    {
                        var rockContext = new RockContext();
                        rockContext.WrapTransaction( () =>
                        {
                            rockContext.Configuration.AutoDetectChangesEnabled = false;
                            rockContext.AttributeValues.AddRange( newAttributeValueList );
                            rockContext.SaveChanges( DisableAudit );
                            newAttributeValueList.Clear();
                        } );
                        ReportPartialProgress();
                    }
                }
            }

            if ( newAttributeValueList.Any() )
            {
                var rockContext = new RockContext();
                rockContext.WrapTransaction( () =>
                {
                    rockContext.Configuration.AutoDetectChangesEnabled = false;
                    rockContext.AttributeValues.AddRange( newAttributeValueList );
                    rockContext.SaveChanges( DisableAudit );
                } );
            }

            ReportProgress( 100, string.Format( "Finished note import: {0:N0} spiritual gifts imported.", completed ) );
        }
    }
}
