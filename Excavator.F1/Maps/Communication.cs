﻿//
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
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Excavator.F1
{
    /// <summary>
    /// Partial of F1Component that holds the Email/Phone # import methods
    /// </summary>
    partial class F1Component
    {
        /// <summary>
        /// Maps the communication data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <returns></returns>
        private void MapCommunication( IQueryable<Row> tableData )
        {
            var attributeService = new AttributeService();
            var numberService = new PhoneNumberService();
            var personService = new PersonService();
            var numberContext = numberService.RockContext.PhoneNumbers;
            var personContext = personService.RockContext.People;

            List<DefinedValue> numberTypeValues = new DefinedValueService().Queryable()
                .Where( dv => dv.DefinedType.Guid == new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) ).ToList();

            // Look up additional Person attributes (existing)
            var personAttributes = attributeService.GetByEntityTypeId( PersonEntityTypeId ).ToList();

            // Add an Attribute for the secondary email
            var secondaryEmailAttributeId = personAttributes.Where( a => a.Key == "SecondaryEmail" ).Select( a => a.Id ).FirstOrDefault();
            if ( secondaryEmailAttributeId == 0 )
            {
                var newSecondaryEmailAttribute = new Rock.Model.Attribute();
                newSecondaryEmailAttribute.Key = "SecondaryEmail";
                newSecondaryEmailAttribute.Name = "Secondary Email";
                newSecondaryEmailAttribute.FieldTypeId = TextFieldTypeId;
                newSecondaryEmailAttribute.EntityTypeId = PersonEntityTypeId;
                newSecondaryEmailAttribute.EntityTypeQualifierValue = string.Empty;
                newSecondaryEmailAttribute.EntityTypeQualifierColumn = string.Empty;
                newSecondaryEmailAttribute.Description = "The secondary email for this person";
                newSecondaryEmailAttribute.DefaultValue = string.Empty;
                newSecondaryEmailAttribute.IsMultiValue = false;
                newSecondaryEmailAttribute.IsRequired = false;
                newSecondaryEmailAttribute.Order = 0;

                attributeService.Add( newSecondaryEmailAttribute, ImportPersonAlias );
                attributeService.Save( newSecondaryEmailAttribute, ImportPersonAlias );
                secondaryEmailAttributeId = newSecondaryEmailAttribute.Id;
            }

            // Add an Attribute for Twitter
            int twitterAttributeId = personAttributes.Where( a => a.Key == "TwitterUsername" ).Select( a => a.Id ).FirstOrDefault();
            if ( twitterAttributeId == 0 )
            {
                var newTwitterAttribute = new Rock.Model.Attribute();
                newTwitterAttribute.Key = "TwitterUsername";
                newTwitterAttribute.Name = "Twitter Username";
                newTwitterAttribute.FieldTypeId = TextFieldTypeId;
                newTwitterAttribute.EntityTypeId = PersonEntityTypeId;
                newTwitterAttribute.EntityTypeQualifierValue = string.Empty;
                newTwitterAttribute.EntityTypeQualifierColumn = string.Empty;
                newTwitterAttribute.Description = "The Twitter username (or link) for this person";
                newTwitterAttribute.DefaultValue = string.Empty;
                newTwitterAttribute.IsMultiValue = false;
                newTwitterAttribute.IsRequired = false;
                newTwitterAttribute.Order = 0;

                attributeService.Add( newTwitterAttribute, ImportPersonAlias );
                attributeService.Save( newTwitterAttribute, ImportPersonAlias );
                twitterAttributeId = newTwitterAttribute.Id;
            }

            // Add an Attribute for Facebook
            var facebookAttributeId = personAttributes.Where( a => a.Key == "FacebookUsername" ).Select( a => a.Id ).FirstOrDefault();
            if ( facebookAttributeId == 0 )
            {
                var newFacebookAttribute = new Rock.Model.Attribute();
                newFacebookAttribute.Key = "FacebookUsername";
                newFacebookAttribute.Name = "Facebook Username";
                newFacebookAttribute.FieldTypeId = TextFieldTypeId;
                newFacebookAttribute.EntityTypeId = PersonEntityTypeId;
                newFacebookAttribute.EntityTypeQualifierValue = string.Empty;
                newFacebookAttribute.EntityTypeQualifierColumn = string.Empty;
                newFacebookAttribute.Description = "The Facebook username (or link) for this person";
                newFacebookAttribute.DefaultValue = string.Empty;
                newFacebookAttribute.IsMultiValue = false;
                newFacebookAttribute.IsRequired = false;
                newFacebookAttribute.Order = 0;

                attributeService.Add( newFacebookAttribute, ImportPersonAlias );
                attributeService.Save( newFacebookAttribute, ImportPersonAlias );
                facebookAttributeId = newFacebookAttribute.Id;
            }

            var secondaryEmailAttribute = AttributeCache.Read( secondaryEmailAttributeId );
            var twitterUsernameAttribute = AttributeCache.Read( twitterAttributeId );
            var facebookUsernameAttribute = AttributeCache.Read( facebookAttributeId );

            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = totalRows / 100;
            ReportProgress( 0, string.Format( "Starting communication import ({0:N0} to import).", totalRows ) );

            foreach ( var row in tableData )
            {
                string value = row["Communication_Value"] as string;
                int? individualId = row["Individual_ID"] as int?;
                int? householdId = row["Household_ID"] as int?;
                int? personId = GetPersonId( individualId, householdId );

                if ( personId != null && !string.IsNullOrWhiteSpace( value ) )
                {
                    DateTime? lastUpdated = row["LastUpdatedDate"] as DateTime?;
                    string communicationComment = row["Communication_Comment"] as string;
                    string type = row["Communication_Type"] as string;
                    bool isListed = (bool)row["Listed"];

                    if ( type.Contains( "Phone" ) || type.Contains( "Mobile" ) )
                    {
                        var extension = string.Empty;
                        int extensionIndex = value.LastIndexOf( 'x' );
                        if ( extensionIndex > 0 )
                        {
                            extension = value.Substring( extensionIndex ).AsNumeric();
                            value = value.Substring( 0, extensionIndex ).AsNumeric();
                        }
                        else
                        {
                            value = value.AsNumeric();
                        }

                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            var numberExists = numberService.GetByPersonId( (int)personId ).Any( n => n.Number.Equals( value ) );
                            if ( !numberExists )
                            {
                                var newNumber = new PhoneNumber();
                                newNumber.CreatedByPersonAliasId = ImportPersonAlias.Id;
                                newNumber.ModifiedDateTime = lastUpdated;
                                newNumber.PersonId = (int)personId;
                                newNumber.IsMessagingEnabled = false;
                                newNumber.IsUnlisted = !isListed;
                                newNumber.Extension = extension.Left( 20 );
                                newNumber.Number = value.Left( 20 );
                                newNumber.Description = communicationComment;

                                newNumber.NumberTypeValueId = numberTypeValues.Where( v => type.StartsWith( v.Name ) )
                                    .Select( v => (int?)v.Id ).FirstOrDefault();

                                numberContext.Add( newNumber );
                                completed++;
                            }
                        }
                    }
                    else
                    {
                        var setNewValues = true;
                        var person = personService.Get( (int)personId );
                        person.Attributes = new Dictionary<string, AttributeCache>();
                        person.AttributeValues = new Dictionary<string, List<AttributeValue>>();

                        if ( value.IsValidEmail() )
                        {
                            string secondaryEmail = string.Empty;
                            if ( string.IsNullOrWhiteSpace( person.Email ) || ( isListed && person.IsEmailActive == false ) )
                            {
                                secondaryEmail = person.Email;
                                person.Email = value.Left( 75 );
                                person.IsEmailActive = isListed;
                                person.DoNotEmail = !isListed;
                                person.ModifiedDateTime = lastUpdated;
                                person.EmailNote = communicationComment;
                            }
                            else if ( !person.Email.Equals( value ) )
                            {
                                secondaryEmail = value;
                            }
                            else
                            {
                                setNewValues = false;
                            }

                            if ( !string.IsNullOrWhiteSpace( secondaryEmail ) )
                            {
                                person.Attributes.Add( "SecondaryEmail", secondaryEmailAttribute );
                                person.AttributeValues.Add( "SecondaryEmail", new List<AttributeValue>() );
                                person.AttributeValues["SecondaryEmail"].Add( new AttributeValue()
                                {
                                    AttributeId = secondaryEmailAttribute.Id,
                                    Value = secondaryEmail
                                } );
                            }
                        }
                        else if ( type.Contains( "Twitter" ) )
                        {
                            person.Attributes.Add( "TwitterUsername", twitterUsernameAttribute );
                            person.AttributeValues.Add( "TwitterUsername", new List<AttributeValue>() );
                            person.AttributeValues["TwitterUsername"].Add( new AttributeValue()
                            {
                                AttributeId = twitterUsernameAttribute.Id,
                                Value = value
                            } );
                        }
                        else if ( type.Contains( "Facebook" ) )
                        {
                            person.Attributes.Add( "FacebookUsername", facebookUsernameAttribute );
                            person.AttributeValues.Add( "FacebookUsername", new List<AttributeValue>() );
                            person.AttributeValues["FacebookUsername"].Add( new AttributeValue()
                            {
                                AttributeId = facebookUsernameAttribute.Id,
                                Value = value
                            } );
                        }
                        else
                        {
                            setNewValues = false;
                        }

                        if ( setNewValues )
                        {
                            RockTransactionScope.WrapTransaction( () =>
                            {
                                personService.Save( person, ImportPersonAlias );

                                foreach ( var attributeCache in person.Attributes.Select( a => a.Value ) )
                                {
                                    string newValue = person.AttributeValues[attributeCache.Key][0].Value ?? string.Empty;
                                    Rock.Attribute.Helper.SaveAttributeValue( person, attributeCache, newValue, ImportPersonAlias );
                                }
                            } );

                            completed++;
                        }
                    }

                    if ( completed % percentage < 1 )
                    {
                        int percentComplete = completed / percentage;
                        ReportProgress( percentComplete, string.Format( "{0:N0} records imported ({1}% complete).", completed, percentComplete ) );
                    }
                    else if ( completed % ReportingNumber < 1 )
                    {
                        numberService.RockContext.SaveChanges();
                        ReportPartialProgress();
                    }
                }
            }

            numberService.RockContext.SaveChanges();
            //personService.RockContext.SaveChanges();
            ReportProgress( 100, string.Format( "Finished communication import: {0:N0} records imported.", completed ) );
        }
    }
}
