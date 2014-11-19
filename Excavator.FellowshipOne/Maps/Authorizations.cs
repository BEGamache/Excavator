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
        /// Maps the authorizations to an attribute.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        private void MapAuthorizations( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            var rockContext = new RockContext();
            var categoryService = new CategoryService( lookupContext );
            var personService = new PersonService( lookupContext );

            var noteList = new List<Note>();

           
            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying Authorization import ({0:N0} found).", totalRows ) );

            var attributeList = new AttributeService( lookupContext ).Queryable().ToList();
            int authorizationAttributeId = 0;
            if ( attributeList.Find( a => a.Key == "PickupAuthorization" ) != null )
            {
                authorizationAttributeId = attributeList.Find( a => a.Key == "PickupAuthorization" ).Id;
            }

            var authorizationAttributeValueList = new List<AttributeValue>();
            authorizationAttributeValueList = new AttributeValueService( rockContext ).Queryable().Where( av => av.AttributeId == authorizationAttributeId ).ToList();

            int f1HouseholdIdAttributeId = attributeList.Find( a => a.Key == "F1HouseholdId" ).Id;

            //places all household attributes in a dictionary where the key is the personId and the houshold is the value.
            var householdDictionary = new AttributeValueService( lookupContext ).Queryable()
                .Where( av => av.AttributeId == f1HouseholdIdAttributeId )
                .Select( av => new { PersonId = av.EntityId, HouseholdId = av.Value } )
                .ToDictionary( t => t.PersonId, t => t.HouseholdId );


            foreach ( var row in tableData )
            {
                //var rockContext = new RockContext();
                var categoryList = new CategoryService( rockContext ).Queryable().ToList();
                

                //check if category exists
                //If it doesn't (though it should), it will create a new category called Authorization
                if ( categoryList.Find( c => c.Name == "Childhood Information" ) == null )
                {
                    var entityType = new EntityTypeService( rockContext );
                    //creates if category doesn't exist
                    var newCategory = new Category();
                    newCategory.IsSystem = false;
                    newCategory.EntityTypeId = entityType.Queryable().Where( e => e.Name == "Rock.Model.Attribute" ).Select( e => e.Id ).FirstOrDefault();
                    newCategory.EntityTypeQualifierColumn = "EntityTypeId";
                    newCategory.EntityTypeQualifierValue = Convert.ToString( PersonEntityTypeId );
                    newCategory.Name = "Authorization";
                    newCategory.Description = "Contains the pickup authorization";

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
                attributeList = new AttributeService( lookupContext ).Queryable().ToList();

                //Check if Attribute exists
                //If it doesn't it creates the attribute
                if ( attributeList.Find( a => a.Key == "PickupAuthorization" ) == null )
                {
                    var fieldType = new FieldTypeService( rockContext );
                    //var newAttributeList = new List<Rock.Model.Attribute>();
                    var fieldTypeId = fieldType.Queryable().Where( e => e.Name == "Memo" ).FirstOrDefault().Id;
                    var category2 = new CategoryService( rockContext ).Queryable().Where( gt => gt.Name == "Childhood Information" ).FirstOrDefault();
                    var category3 = new CategoryService( rockContext ).Queryable().Where( gt => gt.Name == "Authorization" ).FirstOrDefault();

                    //Creates if attribute doesn't exist
                    //The attribute is a memo attribute
                    var newAttribute = new Rock.Model.Attribute();
                    newAttribute.Key = "PickupAuthorization";
                    newAttribute.Name = "Pickup Authorization";
                    newAttribute.FieldTypeId = fieldTypeId;
                    newAttribute.EntityTypeId = PersonEntityTypeId;
                    newAttribute.EntityTypeQualifierValue = string.Empty;
                    newAttribute.EntityTypeQualifierColumn = string.Empty;
                    newAttribute.Description = "Lists who is authorized to pickup this child along with their current phone number.";
                    newAttribute.DefaultValue = string.Empty;
                    newAttribute.IsMultiValue = false;
                    newAttribute.IsRequired = false;

                    if ( categoryList.Find( c => c.Name == "Childhood Information" ) != null )
                    {
                        newAttribute.Categories = new List<Category>();
                        newAttribute.Categories.Add( category2 );

                    }
                        //If authorization category was create, this is where the attribute is set to that category.
                    else
                    {
                        newAttribute.Categories = new List<Category>();
                        newAttribute.Categories.Add( category3 );  //Sets to Authorization Attribute Category.
                    }

                    //saves the attribute
                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.Configuration.AutoDetectChangesEnabled = false;
                        rockContext.Attributes.Add( newAttribute );
                        rockContext.SaveChanges( DisableAudit );
                    } );
                }
                //Adding to the person's attributes
                int? householdId = row["HouseholdID"] as int?;
                string personName = row["PersonName"] as string;
                DateTime? authorizationDate = row["AuthorizationDate"] as DateTime?;

                attributeList = new AttributeService( rockContext ).Queryable().ToList();

                //Gets the Attribute Id for Pickup Authorization.
                authorizationAttributeId = attributeList.Find(a => a.Key == "PickupAuthorization").Id;


                //since F1 authorization applies to the household id and not individual I have to apply it to all members in that household.
                //Value in F1 is a text entry and not a person select. Discuss with staff that we need to break away from this and start using known relationships for authorization
                foreach ( var household in householdDictionary.Where( h => h.Value == Convert.ToString( householdId ) ) )
                {
                    //checks if a record already exists in the list

                    if ( authorizationAttributeValueList.Find( a => a.AttributeId == authorizationAttributeId && a.EntityId == household.Key ) == null )
                    {
                        var person = new PersonService( rockContext ).Queryable().Where( p => p.Id == household.Key ).FirstOrDefault();

                        //trying to keep from adding this attribute to adult records
                        if ( person != null && (person.Age <= 18 || person.Age == null) )
                        {
                            //creates new attribute record if it does not exist.
                            var newAuthorizationAttribute = new AttributeValue();
                            newAuthorizationAttribute.IsSystem = false;
                            newAuthorizationAttribute.AttributeId = authorizationAttributeId;
                            newAuthorizationAttribute.EntityId = household.Key; //the key is the person ID
                            newAuthorizationAttribute.Value = personName + ", "; //text field
                            newAuthorizationAttribute.CreatedDateTime = authorizationDate;

                            //adds the new record to the list
                            authorizationAttributeValueList.Add( newAuthorizationAttribute );
                        }
                    }
                        //if a record already exists
                    else
                    {
                        //adds to the current value.
                        authorizationAttributeValueList.Find( a => a.AttributeId == authorizationAttributeId && a.EntityId == household.Key ).Value = personName + ", ";
                    }
                    
                }
                completed++;
                if ( completed % percentage < 1 )
                {
                    int percentComplete = completed / percentage;
                    ReportProgress( percentComplete, string.Format( "{0:N0} authorizations imported ({1}% complete).", completed, percentComplete ) );
                }
                else if ( completed % ReportingNumber < 1 )
                {
                   //rockContext.WrapTransaction( () =>
                   // {
                   //     rockContext.Configuration.AutoDetectChangesEnabled = false;
                   //     rockContext.AttributeValues.AddRange( authorizationAttributeValueList );
                   //     rockContext.SaveChanges( DisableAudit );  //I get can't insert duplicate entry error
                   // } );

                    ReportPartialProgress();
                }

            }

            if ( authorizationAttributeValueList.Any() )
            {
                //var rockContext = new RockContext();
                rockContext.WrapTransaction( () =>
                {
                    rockContext.Configuration.AutoDetectChangesEnabled = false;
                    rockContext.AttributeValues.AddRange( authorizationAttributeValueList );
                    rockContext.SaveChanges( DisableAudit );
                } );
            }

            ReportProgress( 100, string.Format( "Finished authorization import: {0:N0} notes imported.", completed ) );
        }
    }
}
