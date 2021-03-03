# SliceCore

SLICE cut your big files in chunks or rebuild them from the chunks.

 Syntax to slice   : slice filename [size_of_chunks]
 Syntax to unslice : slice filename.chunk.1
 Default value for size_of_chunks is 5 Mo
			
 To slice the 10 Mo bigvideo.mp4 file in two 5 Mo chunks:
 C:\\> slice bigvideo.mp4
			
 To slice the 10 Mo bigvideo.mp4 file in five 2 Mo chunks:
 C:\\> slice bigvideo.mp4 2000000
			
 To rebuild the bigvideo.mp4 from the chunks:
 C:\\> slice bigvideo.mp4.chunk.1
 
 NB : all the chunks must be in the same folder
