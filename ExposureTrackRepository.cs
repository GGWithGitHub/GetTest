using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Web;

namespace ExposureTrack.Data
{
    public class ExposureTrackRepository : IDisposable
    {

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here. 
                //
            }

            // Free any unmanaged objects here. 
            //
            disposed = true;
        }


		private const double __EarthRadius = 6378.0; // km - average

		public ExposureTrackEntities DB {
			get; private set;
		}

        //lock handle
        private static object _hnd = new Object();

        //Current singleton instance of ExposureTrackRepository
        public static ExposureTrackRepository _current;
        public static ExposureTrackRepository Current
        {
            get {
                //not a web context, use a static instance
                if (HttpContext.Current == null)
                {
                    lock (_hnd)
                    {
                        return _current ?? (_current = new ExposureTrackRepository());
                    }
                }

                //web context, create a context instance
                var etr = HttpContext.Current.Items["__EXPOSURE_TRACK_REPOSITORY__"] as ExposureTrackRepository;
                if (etr == null) {
                    HttpContext.Current.Items["__EXPOSURE_TRACK_REPOSITORY__"] = etr = new ExposureTrackRepository();
                }
                return etr;
            }
            set {
                //not a web context, use a static instance
                if (HttpContext.Current == null)
                {
                    lock (_hnd)
                    {
                        _current = value;
                        return;
                    }
                }

                HttpContext.Current.Items["__EXPOSURE_TRACK_REPOSITORY__"] = value;
            }
        }

		public ExposureTrackRepository() {
			InitializeDB(new ExposureTrackEntities());
		}

		public ExposureTrackRepository(ExposureTrackEntities db)
		{
            InitializeDB(db);
		}

		private void InitializeDB(ExposureTrackEntities db)
		{
            DB = db;
			Notification = new NotificationRepository(db);
			TaskItem = new TaskItemRepository(db);
			SiteUser = new SiteUserRepository(db);
            Device = new DeviceRepository(db);
			Event = new EventRepository(db);
            SensorReading = new SensorReadingRepository(db); //
            SiteUserAlarmLevels = new SiteUserAlarmLevelsRepository(db);
            SiteUserDevice = new SiteUserDeviceRepository(db);
            LicenseAgreements = new LicenseAgreementRepository(db);
            Author = new AuthorRepository(db);
            Instructor = new InstructorRepository(db);
            OrganizationManager = new OrganizationManagerRepository(db);
            CourseR = new CourseRepository(db);
           // ClassRoom = new ClassRoomRepository(db);
            UserAsset = new UserAssetsRepository(db);
            //CourseModuleLookUpTable = new CourseModuleLookupTableRepository(db);
            ScormCourse = new ScormCourseRepository(db);
            Elearning = new StoreProductDigitalRepository(db);
            Transcript = new TranscriptRepository(db);    //Worked for "HazreadyTasks(20sept2014).docx" on 20 sept 2014
            Development = new DevelopmentRepository(db);
            Model = new ModelRepository(db);
        }

		public NotificationRepository Notification { get; private set; }
		public TaskItemRepository TaskItem { get; private set; }
		public SiteUserRepository SiteUser { get; private set; }
        public DeviceRepository Device { get; private set; }
		public EventRepository Event { get; private set; }
        public SensorReadingRepository SensorReading { get; private set; }
        public SiteUserAlarmLevelsRepository SiteUserAlarmLevels { get; private set; }
        public SiteUserDeviceRepository SiteUserDevice { get; private set; }
        public LicenseAgreementRepository LicenseAgreements { get; private set; }
        public AuthorRepository Author { get; private set; }
        public InstructorRepository Instructor { get; private set; }
        public OrganizationManagerRepository OrganizationManager { get; private set; }
        public CourseRepository CourseR { get; private set; }
        //public ClassRoomRepository ClassRoom { get; private set; }
        public UserAssetsRepository UserAsset { get; private set; }
        //public CourseModuleLookupTableRepository CourseModuleLookUpTable { get; private set; }
        public ScormCourseRepository ScormCourse { get; private set; }
        public StoreProductDigitalRepository Elearning { get; private set; }
        public TranscriptRepository Transcript { get; private set; }
        public DevelopmentRepository Development { get; private set; }
        public ModelRepository Model { get; private set; }


        //public void SaveChanges()
        //{
        //    DB.SaveChanges();
        //}



        public int SaveChanges()
        {
            try
            {
                return DB.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new System.Data.Entity.Validation.DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }

        public List<Data.Event> GetInstructorEvents(Guid userId, Guid? eventId)
        {
            if (eventId != null && eventId.ToString() != Guid.Empty.ToString())
            {
                var regs = DB.EventRegistrations.Where(reg => reg.SiteUserId == userId && reg.AttendingRoleId == AttendingRole.Instructor && reg.EventId == eventId).Select(reg => reg.EventId).ToList();

                var instEvents = DB.Events.Where(eve => regs.Contains(eve.EventId)).ToList();

                return instEvents;
            }
            else
            {
                var regs = DB.EventRegistrations.Where(reg => reg.SiteUserId == userId && reg.AttendingRoleId == AttendingRole.Instructor).Select(reg => reg.EventId).ToList();

                var instEvents = DB.Events.Where(eve => regs.Contains(eve.EventId)).ToList();

                return instEvents;
            }

        }


        //No longer applicable. Models should contain event/course/module guids instead of one lookup guid. -Zakk 6/11/2015
        //public Guid GetEventIdByModuleLookupId(Guid guid)
        //{
        //    return Guid.Parse(DB.EventModuleLookups.Where(mod => mod.EventModuleLookupId == guid).Select(mod => mod.EventId).FirstOrDefault().ToString());
        //}
    }
}
