/******************************************************************
 * File:        output.cpp  (Formerly output.c)
 * Description: Output pass
 * Author:					Phil Cheatle
 * Created:					Thu Aug  4 10:56:08 BST 1994
 *
 * (C) Copyright 1994, Hewlett-Packard Ltd.
 ** Licensed under the Apache License, Version 2.0 (the "License");
 ** you may not use this file except in compliance with the License.
 ** You may obtain a copy of the License at
 ** http://www.apache.org/licenses/LICENSE-2.0
 ** Unless required by applicable law or agreed to in writing, software
 ** distributed under the License is distributed on an "AS IS" BASIS,
 ** WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 ** See the License for the specific language governing permissions and
 ** limitations under the License.
 *
 **********************************************************************/

#ifdef _MSC_VER
#pragma warning(disable:4244)  // Conversion warnings
#endif

#include "mfcpch.h"
#include <string.h>
#include <ctype.h>
#ifdef __UNIX__
#include          <assert.h>
#include          <unistd.h>
#include          <errno.h>
#endif
#include "helpers.h"
#include "tfacep.h"
#include "tessvars.h"
#include "ocrclass.h"
#include "control.h"
#include "secname.h"
#include "reject.h"
#include "docqual.h"
#include "output.h"
#include "bestfirst.h"
#include "globals.h"
#include "tesseractclass.h"

#define EPAPER_EXT      ".ep"
#define PAGE_YSIZE      3508
#define CTRL_INSET      '\024'   //dc4=text inset
#define CTRL_FONT       '\016'   //so=font change
#define CTRL_DEFAULT      '\017' //si=default font
#define CTRL_SHIFT      '\022'   //dc2=x shift
#define CTRL_TAB        '\011'   //tab
#define CTRL_NEWLINE      '\012' //newline
#define CTRL_HARDLINE   '\015'   //cr

/**********************************************************************
 * pixels_to_pts
 *
 * Convert an integer number of pixels to the nearest integer
 * number of points.
 **********************************************************************/

inT32 pixels_to_pts(               //convert coords
                    inT32 pixels,
                    inT32 pix_res  //resolution
                   ) {
  float pts;                     //converted value

  pts = pixels * 72.0 / pix_res;
  return (inT32) (pts + 0.5);    //round it
}

namespace tesseract {
void Tesseract::output_pass(  //Tess output pass //send to api
                            PAGE_RES_IT &page_res_it,
                            const TBOX *target_word_box) {
  BLOCK_RES *block_of_last_word;
  inT16 block_id;
  BOOL8 force_eol;               //During output
  BLOCK *nextblock;              //block of next word
  WERD *nextword;                //next word

  page_res_it.restart_page ();
  block_of_last_word = NULL;
  while (page_res_it.word () != NULL) {
    check_debug_pt (page_res_it.word (), 120);

	if (target_word_box)
	{

		TBOX current_word_box=page_res_it.word ()->word->bounding_box();
		FCOORD center_pt((current_word_box.right()+current_word_box.left())/2,(current_word_box.bottom()+current_word_box.top())/2);
		if (!target_word_box->contains(center_pt))
		{
			page_res_it.forward ();
			continue;
		}

	}
    if (tessedit_write_block_separators &&
    block_of_last_word != page_res_it.block ()) {
      block_of_last_word = page_res_it.block ();
      block_id = block_of_last_word->block->index();
    }

    force_eol = (tessedit_write_block_separators &&
      (page_res_it.block () != page_res_it.next_block ())) ||
      (page_res_it.next_word () == NULL);

    if (page_res_it.next_word () != NULL)
      nextword = page_res_it.next_word ()->word;
    else
      nextword = NULL;
    if (page_res_it.next_block () != NULL)
      nextblock = page_res_it.next_block ()->block;
    else
      nextblock = NULL;
                                 //regardless of tilde crunching
    write_results(page_res_it,
                  determine_newline_type(page_res_it.word()->word,
                                         page_res_it.block()->block,
                                         nextword, nextblock), force_eol);
    page_res_it.forward();
  }
}


/*************************************************************************
 * write_results()
 *
 * All recognition and rejection has now been done. Generate the following:
 *   .txt file     - giving the final best choices with NO highlighting
 *   .raw file     - giving the tesseract top choice output for each word
 *   .map file     - showing how the .txt file has been rejected in the .ep file
 *   epchoice list - a list of one element per word, containing the text for the
 *                   epaper. Reject strings are inserted.
 *   inset list    - a list of bounding boxes of reject insets - indexed by the
 *                   reject strings in the epchoice text.
 *************************************************************************/
void Tesseract::write_results(PAGE_RES_IT &page_res_it,
                              char newline_type,  // type of newline
                              BOOL8 force_eol) {  // override tilde crunch?
  WERD_RES *word = page_res_it.word();
  STRING repetition_code;
  const STRING *wordstr;
  STRING wordstr_lengths;
  int i;
  char unrecognised = STRING (unrecognised_char)[0];
  char ep_chars[32];             //Only for unlv_tilde_crunch
  int ep_chars_index = 0;
  char txt_chs[32];              //Only for unlv_tilde_crunch
  char map_chs[32];              //Only for unlv_tilde_crunch
  int txt_index = 0;
  BOOL8 need_reject = FALSE;
  PBLOB_IT blob_it;              //blobs
  UNICHAR_ID space = unicharset.unichar_to_id(" ");
  if ((word->unlv_crunch_mode != CR_NONE ||
       word->best_choice->length() == 0) &&
      !tessedit_zero_kelvin_rejection && !tessedit_word_for_word) {
    if ((word->unlv_crunch_mode != CR_DELETE) &&
        (!stats_.tilde_crunch_written ||
         ((word->unlv_crunch_mode == CR_KEEP_SPACE) &&
          (word->word->space () > 0) &&
          !word->word->flag (W_FUZZY_NON) &&
          !word->word->flag (W_FUZZY_SP)))) {
      if (!word->word->flag (W_BOL) &&
          (word->word->space () > 0) &&
          !word->word->flag (W_FUZZY_NON) &&
          !word->word->flag (W_FUZZY_SP)) {
        // Write a space to separate from preceeding good text.
        txt_chs[txt_index] = ' ';
        map_chs[txt_index++] = '1';
        ep_chars[ep_chars_index++] = ' ';
        stats_.last_char_was_tilde = false;
      }
      need_reject = TRUE;
    }
    if ((need_reject && !stats_.last_char_was_tilde) ||
        (force_eol && stats_.write_results_empty_block)) {
      /* Write a reject char - mark as rejected unless zero_rejection mode */
      stats_.last_char_was_tilde = TRUE;
      txt_chs[txt_index] = unrecognised;
      if (tessedit_zero_rejection || (suspect_level == 0)) {
        map_chs[txt_index++] = '1';
        ep_chars[ep_chars_index++] = unrecognised;
      }
      else {
        map_chs[txt_index++] = '0';
        /*
           The ep_choice string is a faked reject to allow newdiff to sync the
           .etx with the .txt and .map files.
         */
        ep_chars[ep_chars_index++] = CTRL_INSET; // escape code
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
                                 //type
        ep_chars[ep_chars_index++] = 2;
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
      }
      stats_.tilde_crunch_written = true;
      stats_.last_char_was_newline = false;
      stats_.write_results_empty_block = false;
    }

    if ((word->word->flag (W_EOL) && !stats_.last_char_was_newline) || force_eol) {
      /* Add a new line output */
      txt_chs[txt_index] = '\n';
      map_chs[txt_index++] = '\n';
                                 //end line
      ep_chars[ep_chars_index++] = newline_type;

                                 //Cos of the real newline
      stats_.tilde_crunch_written = false;
      stats_.last_char_was_newline = true;
      stats_.last_char_was_tilde = false;
    }
    txt_chs[txt_index] = '\0';
    map_chs[txt_index] = '\0';
    ep_chars[ep_chars_index] = '\0';  // terminate string
    word->ep_choice = new WERD_CHOICE(ep_chars, unicharset);

    if (force_eol)
      stats_.write_results_empty_block = true;
    return;
  }

  /* NORMAL PROCESSING of non tilde crunched words */

  stats_.tilde_crunch_written = false;
  if (newline_type)
    stats_.last_char_was_newline = true;
  else
    stats_.last_char_was_newline = false;
  stats_.write_results_empty_block = force_eol;  // about to write a real word

  if (unlv_tilde_crunching &&
      stats_.last_char_was_tilde &&
      (word->word->space() == 0) &&
      !(word->word->flag(W_REP_CHAR) && tessedit_write_rep_codes) &&
      (word->best_choice->unichar_id(0) == space)) {
    /* Prevent adjacent tilde across words - we know that adjacent tildes within
       words have been removed */
    word->best_choice->remove_unichar_id(0);
    if (word->best_choice->blob_choices() != NULL) {
      BLOB_CHOICE_LIST_C_IT blob_choices_it(word->best_choice->blob_choices());
      if (!blob_choices_it.empty()) delete blob_choices_it.extract();
    }
    word->best_choice->populate_unichars(getDict().getUnicharset());
    word->reject_map.remove_pos (0);
    delete word->box_word;
    word->box_word = new BoxWord;
  }
  if (newline_type ||
    (word->word->flag (W_REP_CHAR) && tessedit_write_rep_codes))
    stats_.last_char_was_tilde = false;
  else {
    if (word->reject_map.length () > 0) {
      if (word->best_choice->unichar_id(word->reject_map.length() - 1) == space)
        stats_.last_char_was_tilde = true;
      else
        stats_.last_char_was_tilde = false;
    }
    else if (word->word->space () > 0)
      stats_.last_char_was_tilde = false;
    /* else it is unchanged as there are no output chars */
  }

  ASSERT_HOST (word->best_choice->length() == word->reject_map.length());

  set_unlv_suspects(word);
  check_debug_pt (word, 120);
  if (tessedit_rejection_debug) {
    tprintf ("Dict word: \"%s\": %d\n",
             word->best_choice->debug_string(unicharset).string(),
             dict_word(*(word->best_choice)));
  }
  if (word->word->flag (W_REP_CHAR) && tessedit_write_rep_codes) {
    repetition_code = "|^~R";
    wordstr_lengths = "\001\001\001\001";
    repetition_code += unicharset.id_to_unichar(get_rep_char (word));
    wordstr_lengths += strlen(unicharset.id_to_unichar(get_rep_char (word)));
    wordstr = &repetition_code;
  } else {
    if (tessedit_zero_rejection) {
      /* OVERRIDE ALL REJECTION MECHANISMS - ONLY REJECT TESS FAILURES */
      for (i = 0; i < word->best_choice->length(); ++i) {
        if (word->reject_map[i].rejected())
          word->reject_map[i].setrej_minimal_rej_accept();
      }
    }
    if (tessedit_minimal_rejection) {
      /* OVERRIDE ALL REJECTION MECHANISMS - ONLY REJECT TESS FAILURES */
      for (i = 0; i < word->best_choice->length(); ++i) {
        if ((word->best_choice->unichar_id(i) != space) &&
            word->reject_map[i].rejected())
          word->reject_map[i].setrej_minimal_rej_accept();
      }
    }
  }
}
}  // namespace tesseract

/**********************************************************************
 * determine_newline_type
 *
 * Find whether we have a wrapping or hard newline.
 * Return FALSE if not at end of line.
 **********************************************************************/

char determine_newline_type(                   //test line ends
                            WERD *word,        //word to do
                            BLOCK *block,      //current block
                            WERD *next_word,   //next word
                            BLOCK *next_block  //block of next word
                           ) {
  inT16 end_gap;                 //to right edge
  inT16 width;                   //of next word
  TBOX word_box;                  //bounding
  TBOX next_box;                  //next word
  TBOX block_box;                 //block bounding

  if (!word->flag (W_EOL))
    return FALSE;                //not end of line
  if (next_word == NULL || next_block == NULL || block != next_block)
    return CTRL_NEWLINE;
  if (next_word->space () > 0)
    return CTRL_HARDLINE;        //it is tabbed
  word_box = word->bounding_box ();
  next_box = next_word->bounding_box ();
  block_box = block->bounding_box ();
                                 //gap to eol
  end_gap = block_box.right () - word_box.right ();
  end_gap -= (inT32) block->space ();
  width = next_box.right () - next_box.left ();
  //      tprintf("end_gap=%d-%d=%d, width=%d-%d=%d, nl=%d\n",
  //              block_box.right(),word_box.right(),end_gap,
  //              next_box.right(),next_box.left(),width,
  //              end_gap>width ? CTRL_HARDLINE : CTRL_NEWLINE);
  return end_gap > width ? CTRL_HARDLINE : CTRL_NEWLINE;
}

/*************************************************************************
 * get_rep_char()
 * Return the first accepted character from the repetition string. This is the
 * character which is repeated - as determined earlier by fix_rep_char()
 *************************************************************************/
namespace tesseract {
UNICHAR_ID Tesseract::get_rep_char(WERD_RES *word) {  // what char is repeated?
  int i;
  for (i = 0; ((i < word->reject_map.length()) &&
               (word->reject_map[i].rejected())); ++i);

  if (i < word->reject_map.length()) {
    return word->best_choice->unichar_id(i);
  } else {
    return unicharset.unichar_to_id(unrecognised_char.string());
  }
}

/*************************************************************************
 * SUSPECT LEVELS
 *
 * 0 - dont reject ANYTHING
 * 1,2 - partial rejection
 * 3 - BEST
 *
 * NOTE: to reject JUST tess failures in the .map file set suspect_level 3 and
 * tessedit_minimal_rejection.
 *************************************************************************/
void Tesseract::set_unlv_suspects(WERD_RES *word_res) {
  int len = word_res->reject_map.length();
  const WERD_CHOICE &word = *(word_res->best_choice);
  int i;
  float rating_per_ch;

  if (suspect_level == 0) {
    for (i = 0; i < len; i++) {
      if (word_res->reject_map[i].rejected())
        word_res->reject_map[i].setrej_minimal_rej_accept();
    }
    return;
  }

  if (suspect_level >= 3)
    return;                      //Use defaults

  /* NOW FOR LEVELS 1 and 2 Find some stuff to unreject*/

  if (safe_dict_word(word) &&
      (count_alphas(word) > suspect_short_words)) {
    /* Unreject alphas in dictionary words */
    for (i = 0; i < len; ++i) {
      if (word_res->reject_map[i].rejected() &&
          unicharset.get_isalpha(word.unichar_id(i)))
        word_res->reject_map[i].setrej_minimal_rej_accept();
    }
  }

  rating_per_ch = word.rating() / word_res->reject_map.length();

  if (rating_per_ch >= suspect_rating_per_ch)
    return;                      //Dont touch bad ratings

  if ((word_res->tess_accepted) || (rating_per_ch < suspect_accept_rating)) {
    /* Unreject any Tess Acceptable word - but NOT tess reject chs*/
    for (i = 0; i < len; ++i) {
      if (word_res->reject_map[i].rejected() &&
          (!unicharset.eq(word.unichar_id(i), " ")))
        word_res->reject_map[i].setrej_minimal_rej_accept();
    }
  }

  for (i = 0; i < len; i++) {
    if (word_res->reject_map[i].rejected()) {
      if (word_res->reject_map[i].flag(R_DOC_REJ))
        word_res->reject_map[i].setrej_minimal_rej_accept();
      if (word_res->reject_map[i].flag(R_BLOCK_REJ))
        word_res->reject_map[i].setrej_minimal_rej_accept();
      if (word_res->reject_map[i].flag(R_ROW_REJ))
        word_res->reject_map[i].setrej_minimal_rej_accept();
    }
  }

  if (suspect_level == 2)
    return;

  if (!suspect_constrain_1Il ||
      (word_res->reject_map.length() <= suspect_short_words)) {
    for (i = 0; i < len; i++) {
      if (word_res->reject_map[i].rejected()) {
        if ((word_res->reject_map[i].flag(R_1IL_CONFLICT) ||
          word_res->reject_map[i].flag(R_POSTNN_1IL)))
          word_res->reject_map[i].setrej_minimal_rej_accept();

        if (!suspect_constrain_1Il &&
          word_res->reject_map[i].flag(R_MM_REJECT))
          word_res->reject_map[i].setrej_minimal_rej_accept();
      }
    }
  }

  if ((acceptable_word_string(word.unichar_string().string(),
                              word.unichar_lengths().string()) !=
       AC_UNACCEPTABLE) ||
      acceptable_number_string(word.unichar_string().string(),
                               word.unichar_lengths().string())) {
    if (word_res->reject_map.length() > suspect_short_words) {
      for (i = 0; i < len; i++) {
        if (word_res->reject_map[i].rejected() &&
          (!word_res->reject_map[i].perm_rejected() ||
           word_res->reject_map[i].flag (R_1IL_CONFLICT) ||
           word_res->reject_map[i].flag (R_POSTNN_1IL) ||
           word_res->reject_map[i].flag (R_MM_REJECT))) {
          word_res->reject_map[i].setrej_minimal_rej_accept();
        }
      }
    }
  }
}

inT16 Tesseract::count_alphas(const WERD_CHOICE &word) {
  int count = 0;
  for (int i = 0; i < word.length(); ++i) {
    if (unicharset.get_isalpha(word.unichar_id(i)))
      count++;
  }
  return count;
}


inT16 Tesseract::count_alphanums(const WERD_CHOICE &word) {
  int count = 0;
  for (int i = 0; i < word.length(); ++i) {
    if (unicharset.get_isalpha(word.unichar_id(i)) ||
        unicharset.get_isdigit(word.unichar_id(i)))
      count++;
  }
  return count;
}


BOOL8 Tesseract::acceptable_number_string(const char *s,
                                          const char *lengths) {
  BOOL8 prev_digit = FALSE;

  if (*lengths == 1 && *s == '(')
    s++;

  if (*lengths == 1 &&
      ((*s == '$') || (*s == '.') || (*s == '+') || (*s == '-')))
    s++;

  for (; *s != '\0'; s += *(lengths++)) {
    if (unicharset.get_isdigit (s, *lengths))
      prev_digit = TRUE;
    else if (prev_digit &&
             (*lengths == 1 && ((*s == '.') || (*s == ',') || (*s == '-'))))
      prev_digit = FALSE;
    else if (prev_digit && *lengths == 1 &&
             (*(s + *lengths) == '\0') && ((*s == '%') || (*s == ')')))
      return TRUE;
    else if (prev_digit &&
             *lengths == 1 && (*s == '%') &&
             (*(lengths + 1) == 1 && *(s + *lengths) == ')') &&
             (*(s + *lengths + *(lengths + 1)) == '\0'))
      return TRUE;
    else
      return FALSE;
  }
  return TRUE;
}





#define EUC_FORMAT_MASK   0xe0
/**********************************************************************
 * ocr_append_char
 *
 * Add a character to the output. Returns OKAY if successful, OCR_API_NO_MEM
 * if there was insufficient room in the buffer.
 **********************************************************************/
inT16 Tesseract::ocr_append_char( /*put char into shm */
					  ETEXT_DESC *monitor,
                      uinT16 char_code,             /*character itself */
                      inT16 left,                   /*of char (-1) */
                      inT16 right,                  /*of char (-1) */
                      inT16 top,                    /*of char (-1) */
                      inT16 bottom,                 /*of char (-1) */
                      inT16 font_index,             /*what font (-1) */
                      uinT8 confidence,             /*0=perfect, 100=reject (0/100) */
                      uinT8 point_size,             /*of char, 72=i inch, (10) */
                      inT8 blanks,                  /*no of spaces before this char (1) */
                      uinT8 enhancement,            /*char enhancement (0) */
                      OCR_CHAR_DIRECTION text_dir,  /*rendering direction (OCR_CDIR_RIGHT_LEFT) */
                      OCR_LINE_DIRECTION line_dir,  /*line rendering direction (OCR_LDIR_DOWN_RIGHT) */
                      OCR_NEWLINE_TYPE nl_type      /*type of newline (if any) (OCR_NL_NONE) */
                     )
{
	if (monitor == NULL)
		return -1;

	ETEXT_DESC* head = (monitor + 1);

	if (head->more_to_come <= 1)
		return -1;	

	if (char_code == ' ' || char_code == '\n' || 
		char_code == '\r'|| char_code == '\t')
		return OCR_API_BAD_CHAR;     /*illegal char */
	

	int fixed_buffer_factor = 100;

	int max_supported_items_count = fixed_buffer_factor * (head->more_to_come - 1);

	int actual_size = (max_supported_items_count)*sizeof(ETEXT_DESC);
	int remain_memory = (actual_size - sizeof (ETEXT_DESC)) / sizeof (EANYCODE_CHAR) - head->count;
	bool isEnoughMemory = remain_memory > 0;
	if (!isEnoughMemory)
		return OCR_API_NO_MEM;       /*insufficient room */

	
	ETEXT_DESC* buf = &head[head->count];
	head->count++;

	int index = 0;          /*count of chars */
                                 /*setup structure */
	buf->text[index].char_code = char_code;
	buf->text[index].left = left;  /*setup structure */
	buf->text[index].right = right;/*setup structure */
	buf->text[index].top = top;    /*setup structure */
	/*setup structure */
	buf->text[index].bottom = bottom;
	/*setup structure */
	buf->text[index].font_index = font_index;
	/*setup structure */
	buf->text[index].confidence = confidence;
	/*setup structure */
	buf->text[index].point_size = point_size;
	/*setup structure */
	buf->text[index].blanks = blanks;

	if (nl_type == OCR_NL_NONE) {
		if (text_dir == OCR_CDIR_TOP_BOTTOM || text_dir == OCR_CDIR_BOTTOM_TOP)
			buf->text[index].formatting = (text_dir << 5) | 128;
		/*setup structure */
		else
			/*setup structure */
			buf->text[index].formatting = text_dir << 5;
	}
	else {
		buf->text[index].formatting = (nl_type << 6) | (line_dir << 5);
		/*setup structure */
	}
	buf->text[index].formatting |= enhancement & (~EUC_FORMAT_MASK);

	buf = NULL;

	return 0;
}




// Write the recognized result to monitor.
void Tesseract::write_results(                    //write output
				  ETEXT_DESC *monitor,
				  WERD_RES *word,     //word to do
				  BLOCK *block,       //block it is from
				  ROW_RES *row,       //row it is from
				  const STRING &text, //text to write
				  const STRING &text_lengths)
{
	inT32 index;                   //char counter
	inT32 index2;                  //char counter
	inT32 length;                  //chars in word
	inT32 ptsize;                  //font size
	inT8 blanks;                   //blanks in word
	uinT8 enhancement;             //bold etc
	uinT8 font;                    //font index
	char unrecognised = '|'; //STRING (unrecognised_char)[0];
	PBLOB *blob;
	TBOX blob_box;                  //bounding box
	PBLOB_IT blob_it;              //blob iterator
	WERD copy_outword;             // copy to denorm
	uinT32 rating;                 //of char
	BOOL8 lineend;                 //end of line
	int offset;
	int offset2;

	ptsize = pixels_to_pts ((inT32) (row->row->x_height () + row->row->ascenders () - row->row->descenders ()), 300);
	if (word->word->flag (W_BOL) && (monitor+1)->more_to_come <= 1)
		//&& ocr_send_text (TRUE) != OKAY)
		return;                      //release failed
	/*copy_outword = *(word->outword);
	copy_outword.baseline_denormalise (&word->denorm);
	blob_it.set_to_list (copy_outword.blob_list ());*/
	blob_it.set_to_list (word->word->gblob_list());
	length = text_lengths.length ();

	if (length > 0) {
		blanks = word->word->space ();
		if (blanks == 0 && tessedit_word_for_word && !word->word->flag (W_BOL))
			blanks = 1;
		for (index = 0, offset = 0; index < length;
			offset += text_lengths[index++], blob_it.forward ()) {
				blob = blob_it.data ();
				blob_box = blob->bounding_box ();

				enhancement = 0;
				if (word->italic > 0)
					enhancement |= EUC_ITALIC;
				if (word->bold > 0)
					enhancement |= EUC_BOLD;
				/*if (tessedit_write_ratings)
					rating = (uinT32) (-word->best_choice->certainty () / 0.035);
				else */if (tessedit_zero_rejection)
					rating = text[offset] == ' ' ? 100 : 0;
				else
					rating = word->reject_map[index].accepted ()? 0 : 100;
				if (rating > 255)
					rating = 255;
				if (word->font1_count > 2)
					font = word->font1;/*
				else if (row->font1_count > 8)
					font = row->font1;*/
				else
					//font index
					font = word->word->flag (W_DONT_CHOP) ? 0 : 1;

				lineend = word->word->flag (W_EOL) && index == length - 1;
				if (word->word->flag (W_EOL) && tessedit_zero_rejection
					&& index < length - 1 && text[index + text_lengths[index]] == ' ') {
						for (index2 = index + 1, offset2 = offset + text_lengths[index];
							index2 < length && text[offset2] == ' ';
							offset2 += text_lengths[index2++]);
						if (index2 == length)
							lineend = TRUE;
				}

				if (!tessedit_zero_rejection || text[offset] != ' '
					|| tessedit_word_for_word) {
						//confidence
						if (text[offset] == ' ') {
							ocr_append_char (
								monitor,
								unrecognised,
								blob_box.left (), blob_box.right (),
								/*page_image.get_ysize () - 1 - blob_box.top (),
								page_image.get_ysize () - 1 - blob_box.bottom (),*/
								pix_grey_->h - 1 - blob_box.top (),
								pix_grey_->h - 1 - blob_box.bottom (),
								font, (uinT8) rating,
								ptsize,                //point size
								blanks, enhancement,   //enhancement
								OCR_CDIR_LEFT_RIGHT,
								OCR_LDIR_DOWN_RIGHT,
								lineend ? OCR_NL_NEWLINE : OCR_NL_NONE);
						} else {
							for (int suboffset = 0; suboffset < text_lengths[index]; ++suboffset)
								ocr_append_char (
								monitor,
								static_cast<unsigned char>(text[offset+suboffset]),
								blob_box.left (), blob_box.right (),
								/*page_image.get_ysize () - 1 - blob_box.top (),
								page_image.get_ysize () - 1 - blob_box.bottom (),*/
								pix_grey_->h - 1 - blob_box.top (),
								pix_grey_->h - 1 - blob_box.bottom (),
								font, (uinT8) rating,
								ptsize,                //point size
								blanks, enhancement,   //enhancement
								OCR_CDIR_LEFT_RIGHT,
								OCR_LDIR_DOWN_RIGHT,
								lineend ? OCR_NL_NEWLINE : OCR_NL_NONE);
						}
						blanks = 0;
				}

		}
	}
	else if (tessedit_word_for_word) {
		blanks = word->word->space ();
		if (blanks == 0 && !word->word->flag (W_BOL))
			blanks = 1;
		blob_box = word->word->bounding_box ();

		enhancement = 0;
		if (word->italic > 0)
			enhancement |= EUC_ITALIC;
		if (word->bold > 0)
			enhancement |= EUC_BOLD;
		rating = 100;
		if (word->font1_count > 2)
			font = word->font1;
		/*else if (row->font1_count > 8)
			font = row->font1;*/
		else
			//font index
			font = word->word->flag (W_DONT_CHOP) ? 0 : 1;

		lineend = word->word->flag (W_EOL);

		//font index
		ocr_append_char (
			monitor,
			unrecognised,
			blob_box.left (), blob_box.right (),
			/*page_image.get_ysize () - 1 - blob_box.top (),
			page_image.get_ysize () - 1 - blob_box.bottom (),*/
			pix_grey_->h - 1 - blob_box.top (),
			pix_grey_->h - 1 - blob_box.bottom (),
			font,
			rating,                    //confidence
			ptsize,                    //point size
			blanks, enhancement,       //enhancement
			OCR_CDIR_LEFT_RIGHT,
			OCR_LDIR_DOWN_RIGHT,
			lineend ? OCR_NL_NEWLINE : OCR_NL_NONE);
	}
}


void Tesseract::write_results(
							  PAGE_RES_IT &page_res_it,
							  char newline_type,  // type of newline
							  BOOL8 force_eol,
							  ETEXT_DESC *monitor)
{
	WERD_RES *word = page_res_it.word();
  STRING repetition_code;
  const STRING *wordstr;
  STRING wordstr_lengths;
  int i;
  char unrecognised = STRING (unrecognised_char)[0];
  char ep_chars[32];             //Only for unlv_tilde_crunch
  int ep_chars_index = 0;
  char txt_chs[32];              //Only for unlv_tilde_crunch
  char map_chs[32];              //Only for unlv_tilde_crunch
  int txt_index = 0;
  BOOL8 need_reject = FALSE;
  PBLOB_IT blob_it;              //blobs
  UNICHAR_ID space = unicharset.unichar_to_id(" ");
  if ((word->unlv_crunch_mode != CR_NONE ||
       word->best_choice->length() == 0) &&
      !tessedit_zero_kelvin_rejection && !tessedit_word_for_word) {
    if ((word->unlv_crunch_mode != CR_DELETE) &&
        (!stats_.tilde_crunch_written ||
         ((word->unlv_crunch_mode == CR_KEEP_SPACE) &&
          (word->word->space () > 0) &&
          !word->word->flag (W_FUZZY_NON) &&
          !word->word->flag (W_FUZZY_SP)))) {
      if (!word->word->flag (W_BOL) &&
          (word->word->space () > 0) &&
          !word->word->flag (W_FUZZY_NON) &&
          !word->word->flag (W_FUZZY_SP)) {
        // Write a space to separate from preceeding good text.
        txt_chs[txt_index] = ' ';
        map_chs[txt_index++] = '1';
        ep_chars[ep_chars_index++] = ' ';
        stats_.last_char_was_tilde = false;
      }
      need_reject = TRUE;
    }
    if ((need_reject && !stats_.last_char_was_tilde) ||
        (force_eol && stats_.write_results_empty_block)) {
      /* Write a reject char - mark as rejected unless zero_rejection mode */
      stats_.last_char_was_tilde = TRUE;
      txt_chs[txt_index] = unrecognised;
      if (tessedit_zero_rejection || (suspect_level == 0)) {
        map_chs[txt_index++] = '1';
        ep_chars[ep_chars_index++] = unrecognised;
      }
      else {
        map_chs[txt_index++] = '0';
        /*
           The ep_choice string is a faked reject to allow newdiff to sync the
           .etx with the .txt and .map files.
         */
        ep_chars[ep_chars_index++] = CTRL_INSET; // escape code
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
                                 //type
        ep_chars[ep_chars_index++] = 2;
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
                                 //dummy reject
        ep_chars[ep_chars_index++] = 1;
      }
      stats_.tilde_crunch_written = true;
      stats_.last_char_was_newline = false;
      stats_.write_results_empty_block = false;
    }

    if ((word->word->flag (W_EOL) && !stats_.last_char_was_newline) || force_eol) {
      /* Add a new line output */
      txt_chs[txt_index] = '\n';
      map_chs[txt_index++] = '\n';
                                 //end line
      ep_chars[ep_chars_index++] = newline_type;

                                 //Cos of the real newline
      stats_.tilde_crunch_written = false;
      stats_.last_char_was_newline = true;
      stats_.last_char_was_tilde = false;
    }
    txt_chs[txt_index] = '\0';
    map_chs[txt_index] = '\0';
    ep_chars[ep_chars_index] = '\0';  // terminate string
    word->ep_choice = new WERD_CHOICE(ep_chars, unicharset);

    if (force_eol)
      stats_.write_results_empty_block = true;
    return;
  }

  /* NORMAL PROCESSING of non tilde crunched words */

  stats_.tilde_crunch_written = false;
  if (newline_type)
    stats_.last_char_was_newline = true;
  else
    stats_.last_char_was_newline = false;
  stats_.write_results_empty_block = force_eol;  // about to write a real word

  if (unlv_tilde_crunching &&
      stats_.last_char_was_tilde &&
      (word->word->space() == 0) &&
      !(word->word->flag(W_REP_CHAR) && tessedit_write_rep_codes) &&
      (word->best_choice->unichar_id(0) == space)) {
    /* Prevent adjacent tilde across words - we know that adjacent tildes within
       words have been removed */
    word->best_choice->remove_unichar_id(0);
    if (word->best_choice->blob_choices() != NULL) {
      BLOB_CHOICE_LIST_C_IT blob_choices_it(word->best_choice->blob_choices());
      if (!blob_choices_it.empty()) delete blob_choices_it.extract();
    }
    word->best_choice->populate_unichars(getDict().getUnicharset());
    word->reject_map.remove_pos (0);
    delete word->box_word;
    word->box_word = new BoxWord;
  }
  if (newline_type ||
    (word->word->flag (W_REP_CHAR) && tessedit_write_rep_codes))
    stats_.last_char_was_tilde = false;
  else {
    if (word->reject_map.length () > 0) {
      if (word->best_choice->unichar_id(word->reject_map.length() - 1) == space)
        stats_.last_char_was_tilde = true;
      else
        stats_.last_char_was_tilde = false;
    }
    else if (word->word->space () > 0)
      stats_.last_char_was_tilde = false;
    /* else it is unchanged as there are no output chars */
  }

  ASSERT_HOST (word->best_choice->length() == word->reject_map.length());

  set_unlv_suspects(word);
  check_debug_pt (word, 120);
  if (tessedit_rejection_debug) {
    tprintf ("Dict word: \"%s\": %d\n",
             word->best_choice->debug_string(unicharset).string(),
             dict_word(*(word->best_choice)));
  }
  if (word->word->flag (W_REP_CHAR) && tessedit_write_rep_codes) {
    repetition_code = "|^~R";
    wordstr_lengths = "\001\001\001\001";
    repetition_code += unicharset.id_to_unichar(get_rep_char (word));
    wordstr_lengths += strlen(unicharset.id_to_unichar(get_rep_char (word)));
    wordstr = &repetition_code;
  } else {

	  wordstr = &(word->best_choice->unichar_string());
	  wordstr_lengths = word->best_choice->unichar_lengths();

    if (tessedit_zero_rejection) {
      /* OVERRIDE ALL REJECTION MECHANISMS - ONLY REJECT TESS FAILURES */
      for (i = 0; i < word->best_choice->length(); ++i) {
        if (word->reject_map[i].rejected())
          word->reject_map[i].setrej_minimal_rej_accept();
      }
    }
    if (tessedit_minimal_rejection) {
      /* OVERRIDE ALL REJECTION MECHANISMS - ONLY REJECT TESS FAILURES */
      for (i = 0; i < word->best_choice->length(); ++i) {
        if ((word->best_choice->unichar_id(i) != space) &&
            word->reject_map[i].rejected())
          word->reject_map[i].setrej_minimal_rej_accept();
      }
    }
  }

  if (monitor != NULL)
  {
	  write_results(monitor, word, page_res_it.block ()->block,
      page_res_it.row (), *wordstr, wordstr_lengths);
  }
}






void Tesseract::output_pass(  //Tess output pass //send to api
							PAGE_RES_IT &page_res_it,
							const TBOX *target_word_box,
							ETEXT_DESC *monitor)
{
	BLOCK_RES *block_of_last_word;
  inT16 block_id;
  BOOL8 force_eol;               //During output
  BLOCK *nextblock;              //block of next word
  WERD *nextword;                //next word

  page_res_it.restart_page ();
  block_of_last_word = NULL;
  while (page_res_it.word () != NULL) {
    check_debug_pt (page_res_it.word (), 120);

	if (target_word_box)
	{

		TBOX current_word_box=page_res_it.word ()->word->bounding_box();
		FCOORD center_pt((current_word_box.right()+current_word_box.left())/2,(current_word_box.bottom()+current_word_box.top())/2);
		if (!target_word_box->contains(center_pt))
		{
			page_res_it.forward ();
			continue;
		}

	}
    if (tessedit_write_block_separators &&
    block_of_last_word != page_res_it.block ()) {
      block_of_last_word = page_res_it.block ();
      block_id = block_of_last_word->block->index();
    }

    force_eol = (tessedit_write_block_separators &&
      (page_res_it.block () != page_res_it.next_block ())) ||
      (page_res_it.next_word () == NULL);

    if (page_res_it.next_word () != NULL)
      nextword = page_res_it.next_word ()->word;
    else
      nextword = NULL;
    if (page_res_it.next_block () != NULL)
      nextblock = page_res_it.next_block ()->block;
    else
      nextblock = NULL;
                                 //regardless of tilde crunching
    /*write_results(page_res_it,
                  determine_newline_type(page_res_it.word()->word,
                                         page_res_it.block()->block,
                                         nextword, nextblock), force_eol);*/

	if (monitor != NULL)
	{
		write_results(
			page_res_it, 
			determine_newline_type (page_res_it.word ()->word, page_res_it.block ()->block, nextword, nextblock), 
			force_eol,
			monitor);
	}

    page_res_it.forward();
  }
}



}  // namespace tesseract
