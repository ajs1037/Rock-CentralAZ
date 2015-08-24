﻿
using System;
using System.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using com.centralaz.DpsMatch.Model;
using com.centralaz.DpsMatch.Data;
using com.centralaz.DpsMatch.Web.UI.Controls.Grid;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;

namespace RockWeb.Plugins.com_centralaz.DpsMatch
{
    /// <summary>
    /// A block to evaluate potential offender matches
    /// </summary>
    [DisplayName( "DPS Evaluation Block" )]
    [Category( "com_centralaz > DpsMatch" )]
    [Description( "Block to manually evaluate Person entries similar to known sexual offenders" )]
    [TextField( "Note Text", "The text for the alert note that is placed on the person's timeline", true, "Known Sex Offender" )]
    [TextField( "Completion Text", "The text for the notification box at completion", true, "DPS Matching Complete" )]
    public partial class DPSEvaluationBlock : Rock.Web.UI.RockBlock
    {
        #region Fields

        static Dictionary<int, List<Match>> _matchList;
        static int _dictionaryIndex = 0;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                if ( _matchList == null )
                {
                    PopulateMatchList();
                }

                if ( _matchList.Count > 0 )
                {
                    BuildColumns();
                    BindGrid();
                }
                else
                {
                    nbComplete.Text = GetAttributeValue( "CompletionText" );
                    nbComplete.Visible = true;
                    lbNext.Visible = false;
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNext_Click( object sender, EventArgs e )
        {
            DpsMatchContext dpsMatchContext = new DpsMatchContext();
            RockContext rockContext = new RockContext();
            MatchService matchService = new MatchService( dpsMatchContext );
            TaggedItemService taggedItemService = new TaggedItemService( rockContext );
            NoteService noteService = new NoteService( rockContext );

            var offenderTag = new TagService( rockContext ).Get( new Guid( "A585EC28-64D7-463F-98E9-B0D957D0DBBC" ) );
            var noteType = new NoteTypeService( rockContext ).Get( Rock.SystemGuid.NoteType.PERSON_TIMELINE_NOTE.AsGuid() );

            foreach ( MatchField matchfield in gValues.Columns.OfType<MatchField>() )
            {
                Match match = matchService.Queryable().Where( m => m.Id == matchfield.MatchId ).FirstOrDefault();
                if ( match.IsMatch == true )
                {
                    if ( taggedItemService.Get( offenderTag.Id, match.PersonAlias.Person.Guid ) == null )
                    {
                        TaggedItem taggedOffender = new TaggedItem();
                        taggedOffender.EntityGuid = match.PersonAlias.Person.Guid;
                        taggedOffender.TagId = offenderTag.Id;
                        taggedItemService.Add( taggedOffender );
                    }

                    Note note = new Note();
                    note.NoteTypeId = noteType.Id;
                    note.EntityId = match.PersonAlias.PersonId;
                    note.IsAlert = true;
                    note.Text = String.Format( "<a href='http://www.icrimewatch.net/results.php?AgencyID=55662&SubmitNameSearch=1&OfndrLast={0}&OfndrFirst={1}&OfndrCity='>{2}</a>", match.Offender.LastName, match.Offender.FirstName, GetAttributeValue( "NoteText" ) );
                    noteService.Add( note );

                }

                match.VerifiedDate = DateTime.Now;
                dpsMatchContext.SaveChanges();

                // The change to the person is done after the DPS MAtch context has saved changes because the person model's PreSaveChanges function can't cast a custom context to a rock context.
                if ( match.PersonAlias.Person.ModifiedDateTime == null || match.PersonAlias.Person.ModifiedDateTime < DateTime.Now.Date )
                {
                    match.PersonAlias.Person.ModifiedDateTime = DateTime.Now.Date;
                }

                rockContext.SaveChanges();
            }

            _dictionaryIndex++;
            if ( ( _matchList.Count - 1 ) >= _dictionaryIndex )
            {
                BuildColumns();
                BindGrid();
                if ( ( _matchList.Count - 2 ) < _dictionaryIndex )
                {
                    lbNext.Text = "Finish";
                }
            }
            else
            {
                _matchList = null;
                _dictionaryIndex = 0;
                lbNext.Visible = false;
                gValues.Visible = false;
                nbComplete.Text = GetAttributeValue( "CompletionText" );
                nbComplete.Visible = true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds the values columns.
        /// </summary>
        private void BuildColumns()
        {
            gValues.Columns.Clear();
            if ( _matchList != null && _matchList.Count > 0 && _matchList.ElementAt( _dictionaryIndex ).Value != null )
            {
                OffenderService offenderService = new OffenderService( new DpsMatchContext() );
                var labelCol = new BoundField();
                labelCol.DataField = "Label";
                labelCol.HeaderStyle.CssClass = "merge-personselect";
                gValues.Columns.Add( labelCol );

                Offender offender = offenderService.Get( _matchList.ElementAt( _dictionaryIndex ).Key );
                var offenderCol = new OffenderField();
                offenderCol.HeaderStyle.CssClass = "merge-personselect";
                offenderCol.DataTextField = string.Format( "property_{0}", offender.Id );
                offenderCol.PersonId = offender.Id;
                offenderCol.ListCount = _matchList.Count;
                offenderCol.CurrentIndex = _dictionaryIndex + 1;
                gValues.Columns.Add( offenderCol );

                var personService = new PersonService( new RockContext() );
                List<Match> matchSubList = _matchList.ElementAt( _dictionaryIndex ).Value;
                Person person = new Person();
                foreach ( Match match in _matchList.ElementAt( _dictionaryIndex ).Value )
                {
                    person = match.PersonAlias.Person;
                    var personCol = new MatchField();
                    personCol.MatchId = match.Id;
                    personCol.HeaderStyle.CssClass = "merge-personselect";
                    personCol.DataTextField = string.Format( "property_{0}", person.Id );
                    personCol.MatchPercentage = match.MatchPercentage;
                    personCol.MatchIsMatch = match.IsMatch;
                    gValues.Columns.Add( personCol );
                }
            }
        }

        /// <summary>
        /// Binds the values.
        /// </summary>
        private void BindGrid()
        {
            if ( _matchList != null && _matchList.Count > 0 && _matchList.ElementAt( _dictionaryIndex ).Value != null )
            {
                OffenderService offenderService = new OffenderService( new DpsMatchContext() );
                Offender offender = offenderService.Get( _matchList.ElementAt( _dictionaryIndex ).Key );
                List<Match> matchSubList = _matchList.ElementAt( _dictionaryIndex ).Value;
                DataTable dt = GetDataTable( offender, matchSubList );
                gValues.DataSource = dt;
                gValues.DataBind();
            }
        }

        /// <summary>
        /// Populates the list of offenders and their potential matches
        /// </summary>
        protected void PopulateMatchList()
        {
            _matchList = new Dictionary<int, List<Match>>();
            List<Match> matchList = new MatchService( new DpsMatchContext() ).Queryable().ToList();
            foreach ( Match match in matchList )
            {
                if ( !match.VerifiedDate.HasValue || match.IsMatch == null || match.IsMatch == true || !match.Offender.VerificationDate.HasValue || match.VerifiedDate < match.Offender.VerificationDate || match.VerifiedDate < match.PersonAlias.Person.ModifiedDateTime )
                {
                    if ( _matchList.ContainsKey( match.OffenderId ) )
                    {
                        _matchList[match.OffenderId].Add( match );
                    }
                    else
                    {
                        _matchList.Add( match.OffenderId, new List<Match>() );
                        _matchList[match.OffenderId].Add( match );
                    }
                }
            }

            // Remove any that already have matches
            foreach ( List<Match> offenderMatchList in _matchList.Values.ToList() )
            {
                if ( offenderMatchList.Where( o => o.IsMatch == true ).Count() > 0 )
                {
                    _matchList.Remove( offenderMatchList.FirstOrDefault().OffenderId );
                }
            }
        }

        /// <summary>
        /// Gets the datatable
        /// </summary>
        /// <param name="offender"> The offender</param>
        /// <param name="matchList">The potential matches for that offender</param>
        /// <returns></returns>
        public DataTable GetDataTable( Offender offender, List<Match> matchList )
        {
            var tbl = new DataTable();

            tbl.Columns.Add( "Label" );

            //Offender
            tbl.Columns.Add( string.Format( "property_{0}", offender.Id ) );

            foreach ( Match match in matchList )
            {
                tbl.Columns.Add( string.Format( "property_{0}", match.PersonAlias.Person.Id ) );
            }

            var rowValues = new List<object>();

            //Name
            rowValues = new List<object>();
            rowValues.Add( "Name" );
            rowValues.Add( String.Format( "{0} {1}", offender.FirstName, offender.LastName ) );
            foreach ( Match match in matchList )
            {
                var person = match.PersonAlias.Person;
                if ( person.NickName != person.FirstName )
                {
                    rowValues.Add( String.Format( "{0}({1}) {2}", person.FirstName, person.NickName, person.LastName ) );
                }
                else
                {
                    rowValues.Add( person.FullName );
                }
            }
            tbl.Rows.Add( rowValues.ToArray() );

            //Physical Description
            rowValues = new List<object>();
            rowValues.Add( "Photo" );
            rowValues.Add( String.Format( "Hair: {0}    Eyes: {1}   Race: {2}", offender.Hair, offender.Eyes, offender.Race ) );
            foreach ( Match match in matchList )
            {
                var person = match.PersonAlias.Person;
                rowValues.Add( Person.GetPhotoImageTag( match.PersonAlias.Person, 65, 65, "merge-photo" ) );
            }
            tbl.Rows.Add( rowValues.ToArray() );

            //Address
            rowValues = new List<object>();
            rowValues.Add( "Address" );
            rowValues.Add( String.Format( "{0}, {1},{2} {3}", offender.ResidentialAddress, offender.ResidentialCity, offender.ResidentialState, offender.ResidentialZip ) );
            foreach ( Match match in matchList )
            {
                rowValues.Add( match.PersonAlias.Person.GetFamilies().FirstOrDefault().GroupLocations.FirstOrDefault().Location.GetFullStreetAddress() );
            }
            tbl.Rows.Add( rowValues.ToArray() );

            //Age
            rowValues = new List<object>();
            rowValues.Add( "Age" );
            rowValues.Add( String.Format( "{0}", offender.Age ) );
            foreach ( Match match in matchList )
            {
                rowValues.Add( String.Format( "{0}", match.PersonAlias.Person.Age ) );
            }
            tbl.Rows.Add( rowValues.ToArray() );

            //Gender
            rowValues = new List<object>();
            rowValues.Add( "Gender" );
            rowValues.Add( String.Format( "{0}", offender.Sex ) );
            foreach ( Match match in matchList )
            {
                if ( match.PersonAlias.Person.Gender == Gender.Male )
                {
                    rowValues.Add( "M" );
                }
                else
                {
                    rowValues.Add( "F" );
                }
            }
            tbl.Rows.Add( rowValues.ToArray() );

            return tbl;
        }

        #endregion
    }
}