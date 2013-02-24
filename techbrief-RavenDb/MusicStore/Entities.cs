using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace techbrief_RavenDb.MusicStore
{
    public class Genre
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Album
    {
        public string Id { get; set; }
        public string AlbumArtUrl { get; set; }
        public string Title { get; set; }
        public int CountSold { get; set; }
        public double Price { get; set; }

        public AlbumGenre Genre { get; set; }
        public AlbumArtist Artist { get; set; }
    }

    public class AlbumGenre
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class AlbumArtist
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
