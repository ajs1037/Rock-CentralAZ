﻿// <copyright>
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Security;

namespace RockWeb.Blocks.Core
{
    /// <summary>
    /// User control for managing the attributes that are available for a specific entity
    /// </summary>
    [DisplayName( "Attributes" )]
    [Category( "Core" )]
    [Description( "Allows for the managing of attribues." )]

    [BooleanField("Configure Type", "Only show attributes for type specified below", true)]
    [EntityTypeField( "Entity", "Entity Name", false, "Applies To", 0 )]
    [TextField( "Entity Qualifier Column", "The entity column to evaluate when determining if this attribute applies to the entity", false, "", "Applies To", 1 )]
    [TextField( "Entity Qualifier Value", "The entity column value to evaluate.  Attributes will only apply to entities with this value", false, "", "Applies To", 2 )]
    [BooleanField( "Allow Setting of Values", "Should UI be available for setting values of the specified Entity ID?", false, "Advanced", 0 )]
    [IntegerField( "Entity Id", "The entity id that values apply to", false, 0, "Advanced", 1 )]
    [BooleanField( "Enable Show In Grid", "Should the 'Show In Grid' option be displayed when editing attributes?", false, "Advanced", 2 )]

    public partial class Attributes : RockBlock
    {
        #region Fields

        protected bool _configuredType = true;
        protected int? _entityTypeId = null;
        protected string _entityQualifierColumn = string.Empty;
        protected string _entityQualifierValue = string.Empty;
        protected bool _displayValueEdit = false;

        protected int? _entityId = null;

        private bool _canConfigure = false;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if (!bool.TryParse( GetAttributeValue( "ConfigureType" ), out _configuredType))
            {
                _configuredType = true;
            }


            bool displayShowInGrid = true;
            if (bool.TryParse(GetAttributeValue("DisplayShowInGrid"), out displayShowInGrid) && displayShowInGrid)
            {
                edtAttribute.ShowInGridVisible = true;
            }
            else
            {
                edtAttribute.ShowInGridVisible = false;
            }

            Guid entityTypeGuid = Guid.Empty;
            if ( Guid.TryParse( GetAttributeValue( "Entity" ), out entityTypeGuid ) )
            {
                _entityTypeId = EntityTypeCache.Read( entityTypeGuid ).Id;
            }
            _entityQualifierColumn = GetAttributeValue( "EntityQualifierColumn" );
            _entityQualifierValue = GetAttributeValue( "EntityQualifierValue" );
            _displayValueEdit = Convert.ToBoolean( GetAttributeValue( "AllowSettingofValues" ) );

            string entityIdString = GetAttributeValue( "EntityId" );
            if ( !string.IsNullOrWhiteSpace( entityIdString ) )
            {
                int entityIdint = 0;
                if ( int.TryParse( entityIdString, out entityIdint ) && entityIdint > 0 )
                {
                    _entityId = entityIdint;
                }
            }

            _canConfigure = IsUserAuthorized( Authorization.ADMINISTRATE );

            rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;

            if ( _canConfigure )
            {
                rGrid.DataKeyNames = new string[] { "id" };
                rGrid.Actions.ShowAdd = true;

                rGrid.Actions.AddClick += rGrid_Add;
                rGrid.GridRebind += rGrid_GridRebind;
                rGrid.RowDataBound += rGrid_RowDataBound;

                rGrid.Columns[1].Visible = !_configuredType;   // qualifier

                rGrid.Columns[4].Visible = !_displayValueEdit; // default value / value
                rGrid.Columns[5].Visible = _displayValueEdit; // default value / value
                rGrid.Columns[6].Visible = _displayValueEdit;  // edit

                SecurityField securityField = rGrid.Columns[7] as SecurityField;
                securityField.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Attribute ) ).Id;

                mdAttribute.SaveClick += mdAttribute_SaveClick;
                mdAttributeValue.SaveClick += mdAttributeValue_SaveClick;

                if ( !_configuredType )
                {
                    var entityTypeList = new EntityTypeService().GetEntities().ToList();
                    ddlEntityType.EntityTypes = entityTypeList;
                    ddlAttrEntityType.EntityTypes = entityTypeList;
                }

                BindFilter();

            }
            else
            {
                nbMessage.Text = "You are not authorized to configure this page";
                nbMessage.Visible = true;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if ( _canConfigure )
                {
                    BindGrid();
                }
            }
            else
            {
                int attributeId = 0;
                if ( hfIdValues.Value != string.Empty && int.TryParse( hfIdValues.Value, out attributeId ) )
                {
                    ShowEditValue( attributeId, false );
                }
                if (hfActiveDialog.Value.ToUpper() == "ATTRIBUTEVALUE")
                {
                }

                ShowDialog();
            }


            base.OnLoad( e );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlEntityType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlEntityType_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindCategoryFilter();
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            if ( !_configuredType )
            {
                rFilter.SaveUserPreference( "Entity Type", ddlEntityType.SelectedValue );
            }

            string categoryFilterValue = cpCategoriesFilter.SelectedValuesAsInt()
                .Where( v => v != 0 )
                .Select( c => c.ToString() )
                .ToList()
                .AsDelimited( "," );

            rFilter.SaveUserPreference( "Categories", categoryFilterValue );

            BindGrid();
        }

        /// <summary>
        /// Rs the filter_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Categories":

                    var categories = new List<string>();

                    foreach ( var idVal in e.Value.SplitDelimitedValues() )
                    {
                        int id = int.MinValue;
                        if ( int.TryParse( idVal, out id ) )
                        {
                            if ( id != 0 )
                            {
                                var category = CategoryCache.Read( id );
                                if ( category != null )
                                {
                                    categories.Add( CategoryCache.Read( id ).Name );
                                }
                            }
                        }
                    }

                    e.Value = categories.AsDelimited( ", " );

                    break;

                case "Entity Type":

                    if ( _configuredType )
                    {
                        e.Value = "";
                    }

                    else
                    {
                        if ( e.Value == "0" )
                        {
                            e.Value = "None (Global Attributes)";
                        }
                        else
                        {
                            e.Value = EntityTypeCache.Read( int.Parse( e.Value ) ).FriendlyName;
                        }
                    }

                    break;

                default:
                    e.Value = "";
                    break;
            }

        }

        /// <summary>
        /// Handles the Edit event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void rGrid_Edit( object sender, RowEventArgs e )
        {
            ShowEdit( (int)rGrid.DataKeys[e.RowIndex]["id"] );
        }

        /// <summary>
        /// Handles the EditValue event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void rGrid_RowSelected( object sender, RowEventArgs e )
        {
            if ( _displayValueEdit )
            {
                ShowEditValue( (int)rGrid.DataKeys[e.RowIndex]["id"], true );
            }
            else
            {
                ShowEdit( (int)rGrid.DataKeys[e.RowIndex]["id"] );
            }
        }

        /// <summary>
        /// Handles the Delete event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void rGrid_Delete( object sender, RowEventArgs e )
        {
            var attributeService = new Rock.Model.AttributeService();

            Rock.Model.Attribute attribute = attributeService.Get( (int)rGrid.DataKeys[e.RowIndex]["id"] );
            if ( attribute != null )
            {
                Rock.Web.Cache.AttributeCache.Flush( attribute.Id );

                attributeService.Delete( attribute, CurrentPersonAlias );
                attributeService.Save( attribute, CurrentPersonAlias );
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the Add event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rGrid_Add( object sender, EventArgs e )
        {
            ShowEdit( 0 );
        }

        /// <summary>
        /// Handles the GridRebind event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rGrid_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs" /> instance containing the event data.</param>
        protected void rGrid_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                int attributeId = (int)rGrid.DataKeys[e.Row.RowIndex].Value;

                var attribute = Rock.Web.Cache.AttributeCache.Read( attributeId );
                var fieldType = Rock.Web.Cache.FieldTypeCache.Read( attribute.FieldTypeId );

                Literal lCategories = e.Row.FindControl( "lCategories" ) as Literal;
                if ( lCategories != null )
                {
                    lCategories.Text = attribute.Categories.Select( c => c.Name ).ToList().AsDelimited( ", " );
                }

                Literal lEntityQualifier = e.Row.FindControl( "lEntityQualifier" ) as Literal;
                if ( lEntityQualifier != null )
                {
                    if ( attribute.EntityTypeId.HasValue )
                    {
                        string entityTypeName = EntityTypeCache.Read( attribute.EntityTypeId.Value ).FriendlyName;
                        if ( !string.IsNullOrWhiteSpace( attribute.EntityTypeQualifierColumn ) )
                        {
                            lEntityQualifier.Text = string.Format( "Where [{0}] = '{1}'", attribute.EntityTypeQualifierColumn, attribute.EntityTypeQualifierValue );
                        }
                        else
                        {
                            lEntityQualifier.Text = entityTypeName;
                        }
                    }
                    else
                    {
                        lEntityQualifier.Text = "Global Attribute";
                    }
                }

                if ( _displayValueEdit )
                {
                    Literal lValue = e.Row.FindControl( "lValue" ) as Literal;
                    if ( lValue != null )
                    {
                        AttributeValueService attributeValueService = new AttributeValueService();
                        var attributeValue = attributeValueService.GetByAttributeIdAndEntityId( attributeId, _entityId ).FirstOrDefault();
                        if ( attributeValue != null && !string.IsNullOrWhiteSpace( attributeValue.Value ) )
                        {
                            lValue.Text = fieldType.Field.FormatValue( lValue, attributeValue.Value, attribute.QualifierValues, true );
                        }
                        else
                        {
                            lValue.Text = string.Format( "<span class='text-muted'>{0}</span>", fieldType.Field.FormatValue( lValue, attribute.DefaultValue, attribute.QualifierValues, true ) );
                        }
                    }
                }
                else
                {
                    Literal lDefaultValue = e.Row.FindControl( "lDefaultValue" ) as Literal;
                    if ( lDefaultValue != null )
                    {
                        lDefaultValue.Text = fieldType.Field.FormatValue( lDefaultValue, attribute.DefaultValue, attribute.QualifierValues, true );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdAttribute control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void mdAttribute_SaveClick( object sender, EventArgs e )
        {
            Rock.Model.Attribute attribute = null;

            if ( _configuredType )
            {
                attribute = Rock.Attribute.Helper.SaveAttributeEdits( edtAttribute,
                    _entityTypeId, _entityQualifierColumn, _entityQualifierValue, CurrentPersonAlias );
            }
            else
            {
                attribute = Rock.Attribute.Helper.SaveAttributeEdits( edtAttribute,
                    ddlAttrEntityType.SelectedValueAsInt(), tbAttrQualifierField.Text, tbAttrQualifierValue.Text, CurrentPersonAlias );
            }

            // Attribute will be null if it was not valid
            if ( attribute == null )
            {
                return;
            }

            HideDialog();

            BindGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the mdAttributeValue control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void mdAttributeValue_SaveClick( object sender, EventArgs e )
        {
            if ( _displayValueEdit )
            {
                int attributeId = 0;
                if ( hfIdValues.Value != string.Empty && !int.TryParse( hfIdValues.Value, out attributeId ) )
                {
                    attributeId = 0;
                }

                if ( attributeId != 0 && fsEditControl.Controls.Count > 0 )
                {
                    var attribute = Rock.Web.Cache.AttributeCache.Read( attributeId );

                    AttributeValueService attributeValueService = new AttributeValueService();
                    var attributeValue = attributeValueService.GetByAttributeIdAndEntityId( attributeId, _entityId ).FirstOrDefault();
                    if ( attributeValue == null )
                    {
                        attributeValue = new Rock.Model.AttributeValue();
                        attributeValue.AttributeId = attributeId;
                        attributeValue.EntityId = _entityId;
                        attributeValueService.Add( attributeValue, CurrentPersonAlias );
                    }

                    var fieldType = Rock.Web.Cache.FieldTypeCache.Read( attribute.FieldType.Id );
                    attributeValue.Value = fieldType.Field.GetEditValue( attribute.GetControl( fsEditControl.Controls[0] ), attribute.QualifierValues );

                    attributeValueService.Save( attributeValue, CurrentPersonAlias );

                    Rock.Web.Cache.AttributeCache.Flush( attributeId );
                    if ( !_entityTypeId.HasValue && _entityQualifierColumn == string.Empty && _entityQualifierValue == string.Empty && !_entityId.HasValue )
                    {
                        Rock.Web.Cache.GlobalAttributesCache.Flush();
                    }
                }

                hfIdValues.Value = string.Empty;

                HideDialog();
            }

            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            ddlEntityType.Visible = !_configuredType;
            ddlEntityType.SelectedValue = rFilter.GetUserPreference( "Entity Type" );
            BindCategoryFilter();
        }

        /// <summary>
        /// Binds the category filter.
        /// </summary>
        private void BindCategoryFilter()
        {
            int? entityTypeId = _configuredType ? _entityTypeId : ddlEntityType.SelectedValueAsInt();

            cpCategoriesFilter.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Attribute ) ).Id;
            cpCategoriesFilter.EntityTypeQualifierColumn = "EntityTypeId";
            cpCategoriesFilter.EntityTypeQualifierValue = entityTypeId.ToString();

            var selectedIDs = new List<int>();

            if ( (entityTypeId ?? 0).ToString() == rFilter.GetUserPreference( "Entity Type" ) )
            {
                foreach ( var idVal in rFilter.GetUserPreference( "Categories" ).SplitDelimitedValues() )
                {
                    int id = int.MinValue;
                    if ( int.TryParse( idVal, out id ) )
                    {
                        selectedIDs.Add( id );
                    }
                }
            }

            cpCategoriesFilter.SetValues( selectedIDs );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            IQueryable<Rock.Model.Attribute> query = null;

            AttributeService attributeService = new AttributeService();
            if ( _configuredType )
            {
                query = attributeService.Get( _entityTypeId, _entityQualifierColumn, _entityQualifierValue);
            }
            else
            {
                int entityTypeId = int.MinValue; 
                if (int.TryParse(rFilter.GetUserPreference("Entity Type"), out entityTypeId))
                {
                    if ( entityTypeId > 0 )
                    {
                        query = attributeService.GetByEntityTypeId( entityTypeId );
                    }
                }
            }

            if (query == null)
            {
                query = attributeService.GetByEntityTypeId( null );
            }

            var selectedCategoryIds = new List<int>();
            rFilter.GetUserPreference( "Categories" ).SplitDelimitedValues().ToList().ForEach( s => selectedCategoryIds.Add( int.Parse( s ) ) );
            if ( selectedCategoryIds.Any() )
            {
                query = query.
                    Where( a => a.Categories.Any( c => selectedCategoryIds.Contains( c.Id ) ) );
            }

            SortProperty sortProperty = rGrid.SortProperty;
            if ( sortProperty != null )
            {
                query = query.
                    Sort( sortProperty );
            }
            else
            {
                query = query.
                    OrderBy( a => a.Key );
            }

            rGrid.DataSource = query.ToList();
            rGrid.DataBind();
        }


        /// <summary>
        /// Shows the edit.
        /// </summary>
        /// <param name="attributeId">The attribute id.</param>
        protected void ShowEdit( int attributeId )
        {
            var attributeModel = new AttributeService().Get( attributeId );

            if ( attributeModel == null )
            {
                mdAttribute.Title = ("Add Attribute").FormatAsHtmlTitle();

                attributeModel = new Rock.Model.Attribute();
                attributeModel.FieldTypeId = FieldTypeCache.Read( Rock.SystemGuid.FieldType.TEXT ).Id;

                if ( !_configuredType )
                {
                    int entityTypeId = int.MinValue;
                    if ( int.TryParse( rFilter.GetUserPreference( "Entity Type" ), out entityTypeId ) && entityTypeId > 0 )
                    {
                        attributeModel.EntityTypeId = entityTypeId;
                    }
                }

                List<int> selectedCategoryIds = cpCategoriesFilter.SelectedValuesAsInt().ToList();
                new CategoryService().Queryable().Where( c => selectedCategoryIds.Contains( c.Id ) ).ToList().ForEach( c =>
                    attributeModel.Categories.Add( c ) );
                edtAttribute.ActionTitle = Rock.Constants.ActionTitle.Add( Rock.Model.Attribute.FriendlyTypeName );
            }
            else
            {
                edtAttribute.ActionTitle = Rock.Constants.ActionTitle.Edit( Rock.Model.Attribute.FriendlyTypeName );
                mdAttribute.Title = ( "Edit " + attributeModel.Name ).FormatAsHtmlTitle();
            }

            Type type = null;
            if ( _entityTypeId.HasValue )
            {
                type = EntityTypeCache.Read( _entityTypeId.Value ).GetEntityType();
            }
            edtAttribute.SetAttributeProperties( attributeModel, type  );

            if ( _configuredType )
            {
                ddlAttrEntityType.Visible = false;
                tbAttrQualifierField.Visible = false;
                tbAttrQualifierValue.Visible = false;
            }
            else
            {
                ddlAttrEntityType.Visible = true;
                tbAttrQualifierField.Visible = true;
                tbAttrQualifierValue.Visible = true;
                
                ddlAttrEntityType.SetValue( attributeModel.EntityTypeId.HasValue ? attributeModel.EntityTypeId.Value.ToString() : "0" );
                tbAttrQualifierField.Text = attributeModel.EntityTypeQualifierColumn;
                tbAttrQualifierValue.Text = attributeModel.EntityTypeQualifierValue;
            }

            ShowDialog( "Attribute", true );
        }

        /// <summary>
        /// Shows the edit value.
        /// </summary>
        /// <param name="attributeId">The attribute id.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        protected void ShowEditValue( int attributeId, bool setValues )
        {
            if ( _displayValueEdit )
            {
                fsEditControl.Controls.Clear();

                var attribute = Rock.Web.Cache.AttributeCache.Read( attributeId );

                mdAttributeValue.Title = attribute.Name + " Value";

                var attributeValue = new AttributeValueService().GetByAttributeIdAndEntityId( attributeId, _entityId ).FirstOrDefault();
                string value = attributeValue != null && !string.IsNullOrWhiteSpace( attributeValue.Value ) ? attributeValue.Value : attribute.DefaultValue;
                attribute.AddControl( fsEditControl.Controls, value, string.Empty, setValues, true );

                SetValidationGroup( fsEditControl.Controls, mdAttributeValue.ValidationGroup );

                if ( setValues )
                {
                    hfIdValues.Value = attribute.Id.ToString();
                    ShowDialog( "AttributeValue", true );
                }
            }
        }

        private void ShowDialog( string dialog, bool setValues = false )
        {
            hfActiveDialog.Value = dialog.ToUpper().Trim();
            ShowDialog( setValues );
        }


        private void ShowDialog( bool setValues = false )
        {
            switch ( hfActiveDialog.Value )
            {
                case "ATTRIBUTE":
                    mdAttribute.Show();
                    break;
                case "ATTRIBUTEVALUE":
                    mdAttributeValue.Show();
                    break;
            }
        }

        private void HideDialog()
        {
            switch ( hfActiveDialog.Value )
            {

                case "ATTRIBUTE":
                    mdAttribute.Hide();
                    break;
                case "ATTRIBUTEVALUE":
                    mdAttributeValue.Hide();
                    break;
            }

            hfActiveDialog.Value = string.Empty;
        }

        #endregion
}
}