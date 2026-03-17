/**
 * \file
 */

#include "config.h"

#include <stdlib.h>
#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_PORTABILITY

#include <dirent.h>

int mono_io_portability_helpers = PORTABILITY_UNKNOWN;

static gchar *mono_portability_find_file_internal (const gchar *pathname, gboolean last_exists);
static gchar *mono_portability_find_wine_file (const gchar *pathname, gboolean last_exists);

static gboolean
mono_portability_is_wine_runtime (void)
{
	const gchar *wineprefix = g_getenv ("WINEPREFIX");
	const gchar *wineloader = g_getenv ("WINELOADER");

	return (wineprefix && *wineprefix) || (wineloader && *wineloader);
}

static gchar *
mono_portability_get_wine_prefix (void)
{
	const gchar *wineprefix = g_getenv ("WINEPREFIX");

	if (wineprefix && *wineprefix)
		return g_strdup (wineprefix);

	if (g_getenv ("WINELOADER"))
		return g_build_filename (g_get_home_dir (), ".wine", NULL);

	return NULL;
}

void mono_portability_helpers_init (void)
{
        const gchar *env;

	if (mono_io_portability_helpers != PORTABILITY_UNKNOWN)
		return;
	
        mono_io_portability_helpers = PORTABILITY_NONE;

	if (mono_portability_is_wine_runtime ())
		mono_io_portability_helpers |= PORTABILITY_DRIVE;
	
        env = g_getenv ("MONO_IOMAP");
        if (env != NULL) {
                /* parse the environment setting and set up some vars
                 * here
                 */
                gchar **options = g_strsplit (env, ":", 0);
                int i;
                
                if (options == NULL) {
                        /* This shouldn't happen */
                        return;
                }
                
                for (i = 0; options[i] != NULL; i++) {
#ifdef DEBUG
                        g_message ("%s: Setting option [%s]", __func__,
                                   options[i]);
#endif
                        if (!strncasecmp (options[i], "drive", 5)) {
                                mono_io_portability_helpers |= PORTABILITY_DRIVE;
                        } else if (!strncasecmp (options[i], "case", 4)) {
                                mono_io_portability_helpers |= PORTABILITY_CASE;
                        } else if (!strncasecmp (options[i], "all", 3)) {
                                mono_io_portability_helpers |= (PORTABILITY_DRIVE | PORTABILITY_CASE);
			}
                }
	}
}

/* Returns newly allocated string, or NULL on failure */
static gchar *find_in_dir (DIR *current, const gchar *name)
{
	struct dirent *entry;

#ifdef DEBUG
	g_message ("%s: looking for [%s]\n", __func__, name);
#endif
	
	while((entry = readdir (current)) != NULL) {
#ifdef DEBUGX
		g_message ("%s: found [%s]\n", __func__, entry->d_name);
#endif
		
		if (!g_ascii_strcasecmp (name, entry->d_name)) {
			char *ret;
			
#ifdef DEBUG
			g_message ("%s: matched [%s] to [%s]\n", __func__,
				   entry->d_name, name);
#endif

			ret = g_strdup (entry->d_name);
			closedir (current);
			return ret;
		}
	}
	
#ifdef DEBUG
	g_message ("%s: returning NULL\n", __func__);
#endif
	
	closedir (current);
	
	return(NULL);
}

gchar *mono_portability_find_file (const gchar *pathname, gboolean last_exists)
{
	gchar *ret;
	
	if (!pathname || !pathname [0])
		return NULL;
	ret = mono_portability_find_file_internal (pathname, last_exists);

	return ret;
}

static gchar *
mono_portability_find_wine_file (const gchar *pathname, gboolean last_exists)
{
	gchar *wineprefix = NULL, *drive_link = NULL, *link_target = NULL;
	gchar *dosdevices_dir = NULL, *drive_root = NULL, *suffix = NULL, *mapped = NULL;
	gchar drive_spec[3];
	const gchar *path_suffix;
	char link_buf[4096];
	char resolved_buf[4096];
	ssize_t link_len;

	if (!pathname || !pathname[0] || !g_ascii_isalpha (pathname[0]) || pathname[1] != ':')
		return NULL;

	if (!mono_portability_is_wine_runtime ())
		return NULL;

	wineprefix = mono_portability_get_wine_prefix ();
	if (!wineprefix)
		return NULL;

	drive_spec[0] = g_ascii_tolower (pathname[0]);
	drive_spec[1] = ':';
	drive_spec[2] = '\0';

	drive_link = g_build_filename (wineprefix, "dosdevices", drive_spec, NULL);
	link_len = readlink (drive_link, link_buf, sizeof (link_buf) - 1);
	if (link_len < 0)
		goto done;
	link_buf[link_len] = '\0';
	link_target = g_strdup (link_buf);

	dosdevices_dir = g_path_get_dirname (drive_link);
	if (realpath (drive_link, resolved_buf) != NULL)
		drive_root = g_strdup (resolved_buf);
	else
		drive_root = g_path_is_absolute (link_target) ? g_strdup (link_target) : g_build_filename (dosdevices_dir, link_target, NULL);
	if (!drive_root)
		goto done;

	path_suffix = pathname + 2;
	while (*path_suffix == '\\' || *path_suffix == '/')
		path_suffix++;

	suffix = g_strdup (path_suffix);
	g_strdelimit (suffix, '\\', '/');

	mapped = suffix[0] ? g_build_filename (drive_root, suffix, NULL) : g_strdup (drive_root);
	if (last_exists && access (mapped, F_OK) != 0) {
		g_free (mapped);
		mapped = NULL;
	}

done:
	g_free (suffix);
	g_free (drive_root);
	g_free (dosdevices_dir);
	g_free (link_target);
	g_free (drive_link);
	g_free (wineprefix);
	return mapped;
}

/* Returns newly-allocated string or NULL on failure */
static gchar *mono_portability_find_file_internal (const gchar *pathname, gboolean last_exists)
{
	gchar *new_pathname, **components, **new_components;
	int num_components = 0, component = 0;
	DIR *scanning = NULL;
	size_t len;

	gchar *wine_path = mono_portability_find_wine_file (pathname, last_exists);
	if (wine_path != NULL)
		return wine_path;

	if (IS_PORTABILITY_NONE) {
		return(NULL);
	}

	new_pathname = g_strdup (pathname);
	
#ifdef DEBUG
	g_message ("%s: Finding [%s] last_exists: %s\n", __func__, pathname,
		   last_exists?"TRUE":"FALSE");
#endif
	
	if (last_exists &&
	    access (new_pathname, F_OK) == 0) {
#ifdef DEBUG
		g_message ("%s: Found it without doing anything\n", __func__);
#endif
		return(new_pathname);
	}
	
	/* First turn '\' into '/' and strip any drive letters */
	g_strdelimit (new_pathname, '\\', '/');

#ifdef DEBUG
	g_message ("%s: Fixed slashes, now have [%s]\n", __func__,
		   new_pathname);
#endif
	
	if (IS_PORTABILITY_DRIVE &&
	    g_ascii_isalpha (new_pathname[0]) &&
	    (new_pathname[1] == ':')) {
		int len = strlen (new_pathname);
		
		g_memmove (new_pathname, new_pathname+2, len - 2);
		new_pathname[len - 2] = '\0';
#ifdef DEBUG
		g_message ("%s: Stripped drive letter, now looking for [%s]\n",
			   __func__, new_pathname);
#endif
	}

	len = strlen (new_pathname);
	if (len > 1 && new_pathname [len - 1] == '/') {
		new_pathname [len - 1] = 0;
#ifdef DEBUG
		g_message ("%s: requested name had a trailing /, rewritten to '%s'\n",
			   __func__, new_pathname);
#endif
	}

	if (last_exists &&
	    access (new_pathname, F_OK) == 0) {
#ifdef DEBUG
		g_message ("%s: Found it\n", __func__);
#endif

		return(new_pathname);
	}

	/* OK, have to work harder.  Take each path component in turn
	 * and do a case-insensitive directory scan for it
	 */

	if (!(IS_PORTABILITY_CASE)) {
		g_free (new_pathname);
		return(NULL);
	}

	components = g_strsplit (new_pathname, "/", 0);
	if (components == NULL) {
		/* This shouldn't happen */
		g_free (new_pathname);
		return(NULL);
	}
	
	while(components[num_components] != NULL) {
		num_components++;
	}
	g_free (new_pathname);
	
	if (num_components == 0){
		return NULL;
	}
	

	new_components = (gchar **)g_new0 (gchar **, num_components + 1);

	if (num_components > 1) {
		if (strcmp (components[0], "") == 0) {
			/* first component blank, so start at / */
			scanning = opendir ("/");
			if (scanning == NULL) {
#ifdef DEBUG
				g_message ("%s: opendir 1 error: %s", __func__,
					   g_strerror (errno));
#endif
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			new_components[component++] = g_strdup ("");
		} else {
			DIR *current;
			gchar *entry;
		
			current = opendir (".");
			if (current == NULL) {
#ifdef DEBUG
				g_message ("%s: opendir 2 error: %s", __func__,
					   g_strerror (errno));
#endif
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			entry = find_in_dir (current, components[0]);
			if (entry == NULL) {
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			scanning = opendir (entry);
			if (scanning == NULL) {
#ifdef DEBUG
				g_message ("%s: opendir 3 error: %s", __func__,
					   g_strerror (errno));
#endif
				g_free (entry);
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			new_components[component++] = entry;
		}
	} else {
		if (last_exists) {
			if (strcmp (components[0], "") == 0) {
				/* First and only component blank */
				new_components[component++] = g_strdup ("");
			} else {
				DIR *current;
				gchar *entry;
				
				current = opendir (".");
				if (current == NULL) {
#ifdef DEBUG
					g_message ("%s: opendir 4 error: %s",
						   __func__,
						   g_strerror (errno));
#endif
					g_strfreev (new_components);
					g_strfreev (components);
					return(NULL);
				}
				
				entry = find_in_dir (current, components[0]);
				if (entry == NULL) {
					g_strfreev (new_components);
					g_strfreev (components);
					return(NULL);
				}
				
				new_components[component++] = entry;
			}
		} else {
				new_components[component++] = g_strdup (components[0]);
		}
	}

#ifdef DEBUG
	g_message ("%s: Got first entry: [%s]\n", __func__, new_components[0]);
#endif

	g_assert (component == 1);
	
	for(; component < num_components; component++) {
		gchar *entry;
		gchar *path_so_far;
		
		if (!last_exists &&
		    component == num_components -1) {
			entry = g_strdup (components[component]);
			closedir (scanning);
		} else {
			entry = find_in_dir (scanning, components[component]);
			if (entry == NULL) {
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		}
		
		new_components[component] = entry;
		
		if (component < num_components -1) {
			path_so_far = g_strjoinv ("/", new_components);

			scanning = opendir (path_so_far);
			g_free (path_so_far);
			if (scanning == NULL) {
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		}
	}
	
	g_strfreev (components);

	new_pathname = g_strjoinv ("/", new_components);

#ifdef DEBUG
	g_message ("%s: pathname [%s] became [%s]\n", __func__, pathname,
		   new_pathname);
#endif
	
	g_strfreev (new_components);

	if ((last_exists &&
	     access (new_pathname, F_OK) == 0) ||
	    (!last_exists)) {
		return(new_pathname);
	}

	g_free (new_pathname);
	return(NULL);
}

#else /* DISABLE_PORTABILITY */

MONO_EMPTY_SOURCE_FILE (mono_io_portability);

#endif /* DISABLE_PORTABILITY */
