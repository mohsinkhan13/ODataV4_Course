using AirVinyl.Api.Helpers;
using AirVinyl.DataAccessLayer;
using AirVinyl.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace AirVinyl.Api.Controllers
{
    public class PeopleController : ODataController
    {
        private AirVinylDbContext _context = new AirVinylDbContext();

        public IHttpActionResult Get()
        {
            return Ok(_context.People);
        }
        public IHttpActionResult Get([FromODataUri]int key)
        {
            var person = _context.People.FirstOrDefault(x => x.PersonId == key);
            if (person == null)
                return NotFound();

            return Ok(person);
        }

        [HttpGet]
        [ODataRoute("People({key})/Email")]
        [ODataRoute("People({key})/FirstName")]
        [ODataRoute("People({key})/LastName")]
        [ODataRoute("People({key})/DateOfBirth")]
        [ODataRoute("People({key})/Gender")]
        public IHttpActionResult GetPersonProperty([FromODataUri] int key)
        {
            var person = _context.People.FirstOrDefault(x => x.PersonId == key);
            if (person == null)
                return NotFound();

            var property = Url.Request.RequestUri.Segments.Last();

            if(!person.HasProperty(property))
                return NotFound();

            var propertyValue = person.GetValue(property);

            if (propertyValue == null)
                return StatusCode(System.Net.HttpStatusCode.NoContent);

            return this.CreateOkHttpActionResult(propertyValue);
        }

        [HttpGet]
        [ODataRoute("People({key})/Friends")]
        [ODataRoute("People({key})/VinylRecords")]
        public IHttpActionResult GetPersonCollectionProperty([FromODataUri] int key)
        {
            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();
            var person = _context.People.Include(collectionPropertyToGet)
                .FirstOrDefault(x => x.PersonId == key);

            if(person == null)
            {
                return NotFound();
            }

            var collectionPropertyValue = person.GetValue(collectionPropertyToGet);

            return this.CreateOkHttpActionResult(collectionPropertyValue);
            
        }

        [HttpGet]
        [ODataRoute("People({key})/Email/$value")]
        [ODataRoute("People({key})/FirstName/$value")]
        [ODataRoute("People({key})/LastName/$value")]
        [ODataRoute("People({key})/DateOfBirth/$value")]
        [ODataRoute("People({key})/Gender/$value")]
        public IHttpActionResult GetPersonPropertyRawValue([FromODataUri] int key)
        {
            var person = _context.People.FirstOrDefault(x => x.PersonId == key);
            if (person == null)
                return NotFound();

            var property = Url.Request.RequestUri
                .Segments[Url.Request.RequestUri.Segments.Length - 2]
                .TrimEnd('/');

            if (!person.HasProperty(property))
                return NotFound();

            var propertyValue = person.GetValue(property);

            if (propertyValue == null)
                return StatusCode(System.Net.HttpStatusCode.NoContent);

            return this.CreateOkHttpActionResult(propertyValue.ToString());

        }


        public IHttpActionResult Post(Person person)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            person.VinylRecords = new List<VinylRecord>();
            person.VinylRecords.Add(new VinylRecord
            {
                Title = "test record",
                Artist = "dfsd"
            });

            _context.People.Add(person);
            _context.SaveChanges();

            return Created(person);
        }

        public IHttpActionResult Put([FromODataUri] int key,Person person)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            var currentPerson = _context.People.FirstOrDefault(x => x.PersonId == key);
            if(currentPerson == null)
            {
                return NotFound();
            }

            person.PersonId = currentPerson.PersonId;

            _context.Entry(currentPerson).CurrentValues.SetValues(person);
            _context.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        public IHttpActionResult Patch([FromODataUri]int key, Delta<Person> person)
        {
            if(!ModelState.IsValid)
            {
                return NotFound();
            }

            var currentPerson = _context.People.FirstOrDefault(x => x.PersonId == key);
            if(currentPerson == null)
            {
                return NotFound();
            }

            person.Patch(currentPerson);
            _context.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);

        }

        public IHttpActionResult Delete([FromODataUri]int key)
        {
            var currentPerson = _context.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            // this person might be another person's friend, we
            // need to this person from their friend collections
            var peopleWithCurrentPersonAsFriend =
                _context.People.Include("Friends")
                .Where(p => p.Friends.Select(f => f.PersonId).AsQueryable().Contains(key));

            foreach (var person in peopleWithCurrentPersonAsFriend.ToList())
            {
                person.Friends.Remove(currentPerson);
            }

            _context.People.Remove(currentPerson);
            _context.SaveChanges();

            // return No Content
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [ODataRoute("People({key})/Friends/$ref")]
        public IHttpActionResult CreateLinkToFriend([FromODataUri]int key,[FromBody] Uri link)
        {
            var currentPerson = _context.People
                .Include("Friends")
                .FirstOrDefault(p => p.PersonId == key);
            if(currentPerson == null)
            {
                return NotFound();
            }

            var keyOfFriendToAdd = Request.GetKeyValue<int>(link);

            if(currentPerson.Friends.Any(x=>x.PersonId == keyOfFriendToAdd))
            {
                return BadRequest("Person already associated");
            }

            var friendToLink = _context.People.FirstOrDefault(x => x.PersonId == keyOfFriendToAdd);
            if(friendToLink == null)
            {
                return NotFound();
            }

            currentPerson.Friends.Add(friendToLink);
            _context.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPut]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult UpdateLinkToFriend([FromODataUri]int key,
            [FromODataUri]int relatedKey,[FromBody] Uri link)
        {
            //find current person from key passed
            var currentPerson = _context.People
                .Include("Friends")
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            //find friend to be replaced from related key passed
            var friendToBeRemoved = _context.People.FirstOrDefault(x => x.PersonId == relatedKey);
            if (friendToBeRemoved == null)
            {
                return NotFound();
            }

            //get id of the new friend to be added - from the odata link in the body
            // and check if already associated with the current person
            var keyOfNewFriendToAdd = Request.GetKeyValue<int>(link);

            if (currentPerson.Friends.Any(x => x.PersonId == keyOfNewFriendToAdd))
            {
                return BadRequest("Person already associated");
            }

            //get new friend
            var newFriendToBeAdded = _context.People.FirstOrDefault(x => x.PersonId == keyOfNewFriendToAdd);

            //remove old friend and add new friend
            currentPerson.Friends.Remove(friendToBeRemoved);
            currentPerson.Friends.Add(newFriendToBeAdded);
            _context.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult DeleteLinkToFriend([FromODataUri]int key,
            [FromODataUri]int relatedKey)
        {
            //find current person from key passed
            var currentPerson = _context.People
                .Include("Friends")
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            //find friend to be deleted from related key passed
            var friendToBeRemoved = _context.People.FirstOrDefault(x => x.PersonId == relatedKey);
            if (friendToBeRemoved == null)
            {
                return NotFound();
            }

            //remove friend 
            currentPerson.Friends.Remove(friendToBeRemoved);
            _context.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            base.Dispose(disposing);
            // 
        }
    }
}
